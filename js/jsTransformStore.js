const databaseName = 'GitTransformer';
const databaseVersion = 1;
const storeName = 'jsTransforms';
const legacyStorageKey = 'JsTransforms';

function openDatabase() {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(databaseName, databaseVersion);

        request.onupgradeneeded = () => {
            const database = request.result;
            if (!database.objectStoreNames.contains(storeName)) {
                database.createObjectStore(storeName, { keyPath: 'name' });
            }
        };

        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
        request.onblocked = () => reject(new Error('The transform database upgrade was blocked.'));
    });
}

function completeTransaction(transaction) {
    return new Promise((resolve, reject) => {
        transaction.oncomplete = () => resolve();
        transaction.onerror = () => reject(transaction.error);
        transaction.onabort = () => reject(transaction.error ?? new Error('The transform database transaction was aborted.'));
    });
}

function completeRequest(request) {
    return new Promise((resolve, reject) => {
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
    });
}

function normalizeTransform(transform) {
    if (!transform) {
        return null;
    }

    const normalized = {
        id: transform.id ?? transform.Id ?? 0,
        addedBy: transform.addedBy ?? transform.AddedBy ?? '',
        name: transform.name ?? transform.Name ?? '',
        code: transform.code ?? transform.Code ?? ''
    };

    return normalized.name ? normalized : null;
}

function transformsMatch(left, right) {
    return left.id === right.id
        && left.addedBy === right.addedBy
        && left.name === right.name
        && left.code === right.code;
}

async function migrateLegacyTransforms(database, defaultTransforms) {
    const legacyJson = localStorage.getItem(legacyStorageKey);
    if (!legacyJson) {
        return;
    }

    let legacyTransforms;
    try {
        legacyTransforms = JSON.parse(legacyJson);
    } catch {
        return;
    }

    if (!Array.isArray(legacyTransforms)) {
        return;
    }

    const defaultsByName = new Map(
        (defaultTransforms ?? [])
            .map(normalizeTransform)
            .filter(Boolean)
            .map(transform => [transform.name, transform]));
    const customTransforms = legacyTransforms
        .map(normalizeTransform)
        .filter(transform => transform && !transformsMatch(transform, defaultsByName.get(transform.name) ?? {}));

    const transaction = database.transaction(storeName, 'readwrite');
    const store = transaction.objectStore(storeName);
    customTransforms.forEach(transform => store.put(transform));
    await completeTransaction(transaction);

    localStorage.removeItem(legacyStorageKey);
}

export async function getAll(defaultTransforms) {
    const database = await openDatabase();
    try {
        await migrateLegacyTransforms(database, defaultTransforms);
        const transaction = database.transaction(storeName, 'readonly');
        return await completeRequest(transaction.objectStore(storeName).getAll());
    } finally {
        database.close();
    }
}

export async function upsert(transform) {
    const normalized = normalizeTransform(transform);
    if (!normalized) {
        throw new Error('A transform name is required.');
    }

    const database = await openDatabase();
    try {
        const transaction = database.transaction(storeName, 'readwrite');
        transaction.objectStore(storeName).put(normalized);
        await completeTransaction(transaction);
    } finally {
        database.close();
    }
}

export async function remove(name) {
    const database = await openDatabase();
    try {
        const transaction = database.transaction(storeName, 'readwrite');
        transaction.objectStore(storeName).delete(name);
        await completeTransaction(transaction);
    } finally {
        database.close();
    }
}
