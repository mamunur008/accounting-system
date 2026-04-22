<script setup lang="ts">
import { apiFetch } from "~/utils/api";

type Account = {
  id: number;
  code?: string | null;
  name: string;
  type?: string | number;
  isPostable?: boolean;
  isActive?: boolean;
};

type FixedAsset = {
  id: number;
  name: string;
  assetAccountId: number;
  accumulatedDeprAccountId: number;
  expenseAccountId: number;
  cost: number;
  salvageValue: number;
  usefulLifeMonths: number;
  startDate: string;
};

const loading = ref(true);
const saving = ref(false);
const error = ref<string | null>(null);
const success = ref<string | null>(null);

const accounts = ref<Account[]>([]);
const assets = ref<FixedAsset[]>([]);

const form = reactive({
  name: "",
  assetAccountId: null as number | null,
  accumulatedDeprAccountId: null as number | null,
  expenseAccountId: null as number | null,
  cost: 0,
  salvageValue: 0,
  usefulLifeMonths: 12,
  startDate: new Date().toISOString().slice(0, 10),
});

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

const monthlyDepreciation = computed(() => {
  const cost = Number(form.cost || 0);
  const salvage = Number(form.salvageValue || 0);
  const life = Number(form.usefulLifeMonths || 0);

  if (life <= 0) return 0;
  const dep = (cost - salvage) / life;
  return dep > 0 ? dep : 0;
});

const postableAccounts = computed(() => {
  return accounts.value.filter((a) => a.isActive && a.isPostable).sort((a, b) => String(a.code ?? "").localeCompare(String(b.code ?? "")));
});

const assetAccounts = computed(() => {
  return postableAccounts.value.filter((a) => {
    const t = typeof a.type === "string" ? a.type.toLowerCase() : a.type;
    return t === 1 || t === "asset";
  });
});

const expenseAccounts = computed(() => {
  return postableAccounts.value.filter((a) => {
    const t = typeof a.type === "string" ? a.type.toLowerCase() : a.type;
    return t === 5 || t === "expense";
  });
});

async function load() {
  loading.value = true;
  error.value = null;
  success.value = null;

  try {
    const [accData, assetData] = await Promise.all([apiFetch<Account[]>(`/api/accounts`), apiFetch<FixedAsset[]>(`/api/fixed-assets`)]);

    accounts.value = accData ?? [];
    assets.value = assetData ?? [];
  } catch (e: any) {
    error.value = normalizeErr(e);
    accounts.value = [];
    assets.value = [];
  } finally {
    loading.value = false;
  }
}

function resetForm() {
  form.name = "";
  form.assetAccountId = null;
  form.accumulatedDeprAccountId = null;
  form.expenseAccountId = null;
  form.cost = 0;
  form.salvageValue = 0;
  form.usefulLifeMonths = 12;
  form.startDate = new Date().toISOString().slice(0, 10);
}

async function save() {
  error.value = null;
  success.value = null;

  if (!form.name.trim()) {
    error.value = "Asset name is required.";
    return;
  }

  if (!form.assetAccountId) {
    error.value = "Asset account is required.";
    return;
  }

  if (!form.accumulatedDeprAccountId) {
    error.value = "Accumulated depreciation account is required.";
    return;
  }

  if (!form.expenseAccountId) {
    error.value = "Depreciation expense account is required.";
    return;
  }

  if (Number(form.cost) <= 0) {
    error.value = "Cost must be greater than 0.";
    return;
  }

  if (Number(form.usefulLifeMonths) <= 0) {
    error.value = "Useful life must be greater than 0.";
    return;
  }

  saving.value = true;

  try {
    await apiFetch(`/api/fixed-assets`, {
      method: "POST",
      body: {
        name: form.name.trim(),
        assetAccountId: form.assetAccountId,
        accumulatedDeprAccountId: form.accumulatedDeprAccountId,
        expenseAccountId: form.expenseAccountId,
        cost: Number(form.cost),
        salvageValue: Number(form.salvageValue || 0),
        usefulLifeMonths: Number(form.usefulLifeMonths),
        startDate: form.startDate,
      },
    });

    success.value = "Fixed asset created successfully.";
    resetForm();
    await load();
  } catch (e: any) {
    error.value = normalizeErr(e);
  } finally {
    saving.value = false;
  }
}

onMounted(load);
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-start justify-between gap-3">
      <div>
        <h1 class="text-2xl font-bold">Fixed Assets</h1>
        <p class="text-sm text-gray-500 dark:text-slate-300">Register depreciable assets and prepare for automatic depreciation journals.</p>
      </div>

      <button class="px-3 py-2 rounded border text-sm border-gray-200 hover:bg-gray-50 dark:border-slate-800 dark:hover:bg-slate-900" @click="load" :disabled="loading">
        {{ loading ? "Loading..." : "Refresh" }}
      </button>
    </div>

    <div class="grid xl:grid-cols-3 gap-6">
      <!-- FORM -->
      <div class="xl:col-span-1">
        <div class="border rounded p-4 space-y-4 dark:border-slate-800">
          <div>
            <h2 class="font-semibold text-lg">Add Fixed Asset</h2>
            <p class="text-xs text-gray-500 dark:text-slate-300 mt-1">This sets up the asset register. Actual monthly depreciation posting comes next.</p>
          </div>

          <div v-if="error" class="p-3 border border-red-200 bg-red-50 text-red-700 rounded text-sm">
            {{ error }}
          </div>

          <div v-if="success" class="p-3 border border-green-200 bg-green-50 text-green-700 rounded text-sm">
            {{ success }}
          </div>

          <div>
            <label class="block text-xs text-gray-500 dark:text-slate-300 mb-1">Asset Name</label>
            <input v-model="form.name" type="text" placeholder="e.g. Office Laptop" class="w-full px-3 py-2 rounded border text-sm border-gray-200 dark:border-slate-800 bg-white dark:bg-slate-950" />
          </div>

          <div>
            <label class="block text-xs text-gray-500 dark:text-slate-300 mb-1">Asset Account</label>
            <select v-model.number="form.assetAccountId" class="w-full px-3 py-2 rounded border text-sm border-gray-200 dark:border-slate-800 bg-white dark:bg-slate-950">
              <option :value="null">-- Select asset account --</option>
              <option v-for="a in assetAccounts" :key="a.id" :value="a.id">{{ a.code ? `${a.code} - ` : "" }}{{ a.name }}</option>
            </select>
          </div>

          <div>
            <label class="block text-xs text-gray-500 dark:text-slate-300 mb-1">Accumulated Depreciation Account</label>
            <select v-model.number="form.accumulatedDeprAccountId" class="w-full px-3 py-2 rounded border text-sm border-gray-200 dark:border-slate-800 bg-white dark:bg-slate-950">
              <option :value="null">-- Select accumulated depreciation account --</option>
              <option v-for="a in assetAccounts" :key="a.id" :value="a.id">{{ a.code ? `${a.code} - ` : "" }}{{ a.name }}</option>
            </select>
          </div>

          <div>
            <label class="block text-xs text-gray-500 dark:text-slate-300 mb-1">Depreciation Expense Account</label>
            <select v-model.number="form.expenseAccountId" class="w-full px-3 py-2 rounded border text-sm border-gray-200 dark:border-slate-800 bg-white dark:bg-slate-950">
              <option :value="null">-- Select expense account --</option>
              <option v-for="a in expenseAccounts" :key="a.id" :value="a.id">{{ a.code ? `${a.code} - ` : "" }}{{ a.name }}</option>
            </select>
          </div>

          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-xs text-gray-500 dark:text-slate-300 mb-1">Cost</label>
              <input v-model.number="form.cost" type="number" min="0" step="0.01" class="w-full px-3 py-2 rounded border text-sm border-gray-200 dark:border-slate-800 bg-white dark:bg-slate-950" />
            </div>

            <div>
              <label class="block text-xs text-gray-500 dark:text-slate-300 mb-1">Salvage Value</label>
              <input v-model.number="form.salvageValue" type="number" min="0" step="0.01" class="w-full px-3 py-2 rounded border text-sm border-gray-200 dark:border-slate-800 bg-white dark:bg-slate-950" />
            </div>
          </div>

          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-xs text-gray-500 dark:text-slate-300 mb-1">Useful Life (Months)</label>
              <input v-model.number="form.usefulLifeMonths" type="number" min="1" step="1" class="w-full px-3 py-2 rounded border text-sm border-gray-200 dark:border-slate-800 bg-white dark:bg-slate-950" />
            </div>

            <div>
              <label class="block text-xs text-gray-500 dark:text-slate-300 mb-1">Start Date</label>
              <input v-model="form.startDate" type="date" class="w-full px-3 py-2 rounded border text-sm border-gray-200 dark:border-slate-800 bg-white dark:bg-slate-950" />
            </div>
          </div>

          <div class="p-3 rounded bg-gray-50 dark:bg-slate-900">
            <div class="text-xs text-gray-500 dark:text-slate-300">Monthly Depreciation</div>
            <div class="font-semibold text-lg">
              {{ monthlyDepreciation.toFixed(2) }}
            </div>
          </div>

          <div class="flex gap-2">
            <button class="px-4 py-2 rounded bg-black text-white text-sm hover:bg-gray-900 dark:bg-white dark:text-black dark:hover:bg-gray-100" @click="save" :disabled="saving">
              {{ saving ? "Saving..." : "Create Fixed Asset" }}
            </button>

            <button type="button" class="px-4 py-2 rounded border text-sm border-gray-200 hover:bg-gray-50 dark:border-slate-800 dark:hover:bg-slate-900" @click="resetForm">Reset</button>
          </div>
        </div>
      </div>

      <!-- LIST -->
      <div class="xl:col-span-2">
        <div class="border rounded overflow-x-auto dark:border-slate-800">
          <table class="min-w-full text-sm">
            <thead>
              <tr class="bg-gray-50 dark:bg-slate-900">
                <th class="p-2 text-left text-gray-700 dark:text-slate-200">Name</th>
                <th class="p-2 text-right text-gray-700 dark:text-slate-200">Cost</th>
                <th class="p-2 text-right text-gray-700 dark:text-slate-200">Salvage</th>
                <th class="p-2 text-right text-gray-700 dark:text-slate-200">Life</th>
                <th class="p-2 text-right text-gray-700 dark:text-slate-200">Monthly Dep.</th>
                <th class="p-2 text-left text-gray-700 dark:text-slate-200">Start Date</th>
              </tr>
            </thead>

            <tbody>
              <tr v-for="a in assets" :key="a.id" class="border-t border-gray-200 dark:border-slate-800">
                <td class="p-2">{{ a.name }}</td>
                <td class="p-2 text-right tabular-nums">{{ Number(a.cost).toFixed(2) }}</td>
                <td class="p-2 text-right tabular-nums">{{ Number(a.salvageValue).toFixed(2) }}</td>
                <td class="p-2 text-right tabular-nums">{{ a.usefulLifeMonths }}</td>
                <td class="p-2 text-right tabular-nums">
                  {{ ((Number(a.cost) - Number(a.salvageValue)) / Number(a.usefulLifeMonths || 1)).toFixed(2) }}
                </td>
                <td class="p-2">{{ a.startDate }}</td>
              </tr>

              <tr v-if="!loading && assets.length === 0" class="border-t border-gray-200 dark:border-slate-800">
                <td class="p-3 text-gray-500 dark:text-slate-300" colspan="6">No fixed assets yet.</td>
              </tr>
            </tbody>
          </table>
        </div>

        <p class="mt-3 text-xs text-gray-500 dark:text-slate-300">Next step: add a backend action that generates the monthly depreciation journal automatically.</p>
      </div>
    </div>
  </div>
</template>
