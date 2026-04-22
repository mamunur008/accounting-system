<script setup lang="ts">
import { apiFetch } from "~/utils/api";
import AccountTree from "~/components/AccountTree.vue";

type AccountNode = {
  id: number;
  code?: string | null;
  name: string;
  type?: string | number;
  parentId?: number | null;
  isPostable?: boolean;
  isActive?: boolean;
  children?: AccountNode[];
};

type AccountRow = {
  id: number;
  code?: string | null;
  name: string;
  type?: string | number;
  parentId?: number | null;
  isPostable?: boolean;
  isActive?: boolean;
};

const loading = ref(true);
const saving = ref(false);

const error = ref<string | null>(null);
const success = ref<string | null>(null);

const tree = ref<AccountNode[]>([]);
const flat = ref<AccountRow[]>([]);

const expanded = ref<Record<number, boolean>>({});

const form = reactive({
  code: "",
  name: "",
  type: 1,
  parentId: null as number | null,
  isPostable: true,
  isActive: true,
});

const typeOptions = [
  { value: 1, label: "Asset" },
  { value: 2, label: "Liability" },
  { value: 3, label: "Equity" },
  { value: 4, label: "Income" },
  { value: 5, label: "Expense" },
];

function normalizeErr(e: any) {
  const msg = e?.data?.message ?? e?.data ?? e?.message ?? "Request failed";
  return typeof msg === "string" ? msg : JSON.stringify(msg);
}

function typeLabel(t: any) {
  if (typeof t === "string") return t;
  if (t === 1) return "Asset";
  if (t === 2) return "Liability";
  if (t === 3) return "Equity";
  if (t === 4) return "Income";
  if (t === 5) return "Expense";
  return String(t ?? "—");
}

function toggle(id: number) {
  expanded.value[id] = !expanded.value[id];
}

function isOpen(id: number) {
  return expanded.value[id] ?? true;
}

async function load() {
  loading.value = true;
  error.value = null;
  success.value = null;

  try {
    const [treeData, flatData] = await Promise.all([apiFetch<AccountNode[]>(`/api/accounts/tree`), apiFetch<AccountRow[]>(`/api/accounts`)]);

    tree.value = treeData ?? [];
    flat.value = flatData ?? [];

    for (const n of tree.value) {
      expanded.value[n.id] = true;
    }
  } catch (e: any) {
    error.value = normalizeErr(e);
  } finally {
    loading.value = false;
  }
}

function resetForm() {
  form.code = "";
  form.name = "";
  form.type = 1;
  form.parentId = null;
  form.isPostable = true;
  form.isActive = true;
}

async function save() {
  error.value = null;
  success.value = null;

  if (!form.code.trim()) {
    error.value = "Code is required";
    return;
  }

  if (!form.name.trim()) {
    error.value = "Name is required";
    return;
  }

  saving.value = true;

  try {
    await apiFetch(`/api/accounts`, {
      method: "POST",
      body: {
        code: form.code.trim(),
        name: form.name.trim(),
        type: Number(form.type),
        parentId: form.parentId,
        isPostable: form.isPostable,
        isActive: form.isActive,
      },
    });

    success.value = "Account created";
    resetForm();
    await load();
  } catch (e: any) {
    error.value = normalizeErr(e);
  } finally {
    saving.value = false;
  }
}

const parentOptions = computed(() => {
  return flat.value.filter((a) => a.isActive).sort((a, b) => String(a.code ?? "").localeCompare(String(b.code ?? "")));
});

onMounted(load);
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-start justify-between">
      <div>
        <h1 class="text-2xl font-bold">Accounts</h1>
        <p class="text-sm text-gray-500 dark:text-slate-300">Manage your chart of accounts.</p>
      </div>

      <button class="px-3 py-2 rounded border text-sm dark:border-slate-800" @click="load">Refresh</button>
    </div>

    <div class="grid xl:grid-cols-3 gap-6">
      <!-- CREATE ACCOUNT -->

      <div class="border rounded p-4 space-y-4 dark:border-slate-800">
        <h2 class="font-semibold">Create Account</h2>

        <div v-if="error" class="text-red-600 text-sm">
          {{ error }}
        </div>

        <div v-if="success" class="text-green-600 text-sm">
          {{ success }}
        </div>

        <div>
          <label class="text-xs text-gray-500">Code</label>
          <input v-model="form.code" class="w-full border rounded px-3 py-2 text-sm dark:border-slate-800 dark:bg-slate-950" />
        </div>

        <div>
          <label class="text-xs text-gray-500">Name</label>
          <input v-model="form.name" class="w-full border rounded px-3 py-2 text-sm dark:border-slate-800 dark:bg-slate-950" />
        </div>

        <div>
          <label class="text-xs text-gray-500">Type</label>
          <select v-model.number="form.type" class="w-full border rounded px-3 py-2 text-sm dark:border-slate-800 dark:bg-slate-950">
            <option v-for="t in typeOptions" :key="t.value" :value="t.value">
              {{ t.label }}
            </option>
          </select>
        </div>

        <div>
          <label class="text-xs text-gray-500">Parent</label>

          <select v-model.number="form.parentId" class="w-full border rounded px-3 py-2 text-sm dark:border-slate-800 dark:bg-slate-950">
            <option :value="null">None</option>

            <option v-for="a in parentOptions" :key="a.id" :value="a.id">{{ a.code }} - {{ a.name }}</option>
          </select>
        </div>

        <label class="flex gap-2 text-sm">
          <input type="checkbox" v-model="form.isPostable" />
          Postable
        </label>

        <label class="flex gap-2 text-sm">
          <input type="checkbox" v-model="form.isActive" />
          Active
        </label>

        <div class="flex gap-2">
          <button class="px-4 py-2 rounded bg-black text-white text-sm dark:bg-white dark:text-black" @click="save" :disabled="saving">
            {{ saving ? "Saving..." : "Create" }}
          </button>

          <button class="px-4 py-2 rounded border text-sm dark:border-slate-800" @click="resetForm">Reset</button>
        </div>
      </div>

      <!-- ACCOUNT TREE -->

      <div class="xl:col-span-2 border rounded p-4 dark:border-slate-800">
        <h2 class="font-semibold mb-4">Chart of Accounts</h2>

        <div v-if="loading" class="text-sm text-gray-500">Loading accounts...</div>

        <AccountTree v-if="!loading" :nodes="tree" :level="0" :isOpen="isOpen" :toggle="toggle" :typeLabel="typeLabel" />
      </div>
    </div>
  </div>
</template>
