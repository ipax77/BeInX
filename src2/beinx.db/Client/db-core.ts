const DB_NAME = "BeInXDB";
export const DB_VERSION = 1;

export const STORES = {
    payments: "PaymentMeans",
    buyers: "Buyers",
    sellers: "Sellers",
    invoices: "Invoices",
    config: "AppConfig",
    drafts: "Drafts",
};

export type Dump = {
    __meta: { dbVersion: number; date: string };
    stores: Record<string, unknown[]>;
};

export type Migration = {
  schema?: (db: IDBDatabase, tx: IDBTransaction) => void;
  data?: (dump: Dump) => Dump;
};

let db: IDBDatabase | null = null;

export function openDB(): Promise<IDBDatabase> {
    return new Promise((resolve, reject) => {
        if (db) {
            resolve(db);
            return;
        }

        const request = indexedDB.open(DB_NAME, DB_VERSION);

        request.onupgradeneeded = (event) => {
            const database = (event.target as IDBOpenDBRequest).result;
            const tx = (event.target as IDBOpenDBRequest).transaction!;
            const oldVersion = event.oldVersion;

            // Apply migrations incrementally
            for (let v = oldVersion; v < DB_VERSION; v++) {
                const migration = upgrades[v];
                if (migration?.schema) {
                    migration.schema(database, tx);
                }
            }
        };

        request.onsuccess = () => {
            db = request.result;

            resolve(db);
        };

        request.onerror = () => reject(request.error);
    });
}

export function closeDB(): void {
    if (db) {
        db.close();
        db = null;
    }
}

export function migrateDump(dump: Dump): Dump {
  let version = dump.__meta.dbVersion;

  while (version < DB_VERSION) {
    const migration = upgrades[version];

    if (migration?.data) {
      dump = migration.data(dump);
    } else {
      dump.__meta.dbVersion = version + 1;
    }

    version = dump.__meta.dbVersion;
  }

  return dump;
}

export const migration0: Migration = {
    schema: (db, tx) => {
        if (!db.objectStoreNames.contains(STORES.payments)) {
            db.createObjectStore(STORES.payments, { keyPath: "id", autoIncrement: true });
        }

        if (!db.objectStoreNames.contains(STORES.config)) {
            db.createObjectStore(STORES.config);
        }

        if (!db.objectStoreNames.contains(STORES.drafts)) {
            db.createObjectStore(STORES.drafts, { keyPath: "id" });
        }

        if (!db.objectStoreNames.contains(STORES.buyers)) {
            db.createObjectStore(STORES.buyers, { keyPath: "id", autoIncrement: true });
        }

        if (!db.objectStoreNames.contains(STORES.sellers)) {
            db.createObjectStore(STORES.sellers, { keyPath: "id", autoIncrement: true });
        }

        if (!db.objectStoreNames.contains(STORES.invoices)) {
            const store = db.createObjectStore(STORES.invoices, { keyPath: "id", autoIncrement: true });
            store.createIndex("year", "year", { unique: false });
            store.createIndex("isPaid", "isPaid", { unique: false });
            store.createIndex("isImported", "isImported", { unique: false });
        }
    },
};

const upgrades: Record<number, Migration> = {
  0: migration0, // initial schema
};