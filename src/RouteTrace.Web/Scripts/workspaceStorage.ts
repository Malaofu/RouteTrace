interface StoredWorkspaceRecord {
    id: string;
    name: string;
    payload: string;
    updatedAt?: number;
}

interface SavedWorkspaceSummary {
    id: string;
    name: string;
}

const databaseName = "route-trace";
const storeName = "workspaces";

function openDatabase(): Promise<IDBDatabase> {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(databaseName, 1);
        request.onupgradeneeded = () => {
            if (!request.result.objectStoreNames.contains(storeName)) {
                request.result.createObjectStore(storeName, { keyPath: "id" });
            }
        };
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
    });
}

function requestResult<T>(request: IDBRequest<T>): Promise<T> {
    return new Promise((resolve, reject) => {
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
    });
}

export async function listWorkspaces(): Promise<SavedWorkspaceSummary[]> {
    const database = await openDatabase();
    try {
        const records = await requestResult(database.transaction(storeName, "readonly").objectStore(storeName).getAll()) as StoredWorkspaceRecord[];
        return records.map(record => ({ id: record.id, name: record.name }))
            .sort((left, right) => left.name.localeCompare(right.name));
    } finally {
        database.close();
    }
}

export async function saveWorkspace(record: StoredWorkspaceRecord): Promise<void> {
    const database = await openDatabase();
    try {
        await requestResult(database.transaction(storeName, "readwrite").objectStore(storeName).put({
            ...record,
            updatedAt: Date.now(),
        }));
    } finally {
        database.close();
    }
}

export async function openMostRecentWorkspace(): Promise<string | null> {
    const database = await openDatabase();
    try {
        const records = await requestResult(
            database.transaction(storeName, "readonly").objectStore(storeName).getAll()) as StoredWorkspaceRecord[];
        const mostRecent = records.sort((left, right) => (right.updatedAt ?? 0) - (left.updatedAt ?? 0))[0];
        return mostRecent?.payload ?? null;
    } finally {
        database.close();
    }
}

export async function openWorkspace(id: string): Promise<string | null> {
    const database = await openDatabase();
    try {
        const record = await requestResult(database.transaction(storeName, "readonly").objectStore(storeName).get(id)) as StoredWorkspaceRecord | undefined;
        return record?.payload ?? null;
    } finally {
        database.close();
    }
}

export async function deleteWorkspace(id: string): Promise<void> {
    const database = await openDatabase();
    try {
        await requestResult(database.transaction(storeName, "readwrite").objectStore(storeName).delete(id));
    } finally {
        database.close();
    }
}
