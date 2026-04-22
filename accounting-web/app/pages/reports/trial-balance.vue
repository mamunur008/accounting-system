<script setup lang="ts">
const config = useRuntimeConfig();

function apiUrl(path: string) {
  const base = String(config.public.apiBase || "").replace(/\/+$/, "");
  const p = path.startsWith("/") ? path : `/${path}`;
  return `${base}${p}`;
}
async function apiFetch<T>(path: string, opts: any = {}) {
  return await $fetch<T>(apiUrl(path), opts);
}

type TrialRow = { accountId?: number; id?: number; code?: string | null; name?: string | null; debit?: number; credit?: number };
type TrialResponse = TrialRow[] | { rows?: TrialRow[]; items?: TrialRow[]; data?: TrialRow[]; totalDebit?: number; totalCredit?: number };

const loading = ref(true);
const error = ref<string | null>(null);

const rows = ref<TrialRow[]>([]);
const totalDebit = ref(0);
const totalCredit = ref(0);

function n(v: any) {
  const x = Number(v ?? 0);
  return Number.isFinite(x) ? x : 0;
}
function money(v: any) {
  return n(v).toFixed(2);
}
function normalizeErr(e: any) {
  const msg = e?.data?.message ?? e?.data ?? e?.message ?? "Request failed";
  return typeof msg === "string" ? msg : JSON.stringify(msg);
}
function unpack(resp: TrialResponse) {
  if (Array.isArray(resp)) return { list: resp, tD: undefined as number | undefined, tC: undefined as number | undefined };
  const list = resp.rows ?? resp.items ?? resp.data ?? [];
  return { list, tD: resp.totalDebit, tC: resp.totalCredit };
}

async function load() {
  loading.value = true;
  error.value = null;

  try {
    const resp = await apiFetch<TrialResponse>(`/api/reports/trial-balance`);
    const { list, tD, tC } = unpack(resp);

    rows.value = (list ?? []).map((r) => ({ ...r, debit: n(r.debit), credit: n(r.credit) }));

    if (typeof tD === "number" || typeof tC === "number") {
      totalDebit.value = n(tD);
      totalCredit.value = n(tC);
    } else {
      totalDebit.value = rows.value.reduce((sum, r) => sum + n(r.debit), 0);
      totalCredit.value = rows.value.reduce((sum, r) => sum + n(r.credit), 0);
    }
  } catch (e: any) {
    error.value = normalizeErr(e);
    rows.value = [];
    totalDebit.value = 0;
    totalCredit.value = 0;
  } finally {
    loading.value = false;
  }
}

onMounted(load);
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-start justify-between gap-3">
      <div>
        <h1 class="text-2xl font-bold">Trial Balance</h1>
        <p class="text-sm text-gray-500 dark:text-slate-300">Posted journals only</p>
      </div>

      <button class="px-3 py-2 rounded border text-sm border-gray-200 hover:bg-gray-50 dark:border-slate-800 dark:hover:bg-slate-900" @click="load" :disabled="loading">
        {{ loading ? "Loading..." : "Refresh" }}
      </button>
    </div>

    <div v-if="error" class="p-3 border border-red-200 bg-red-50 text-red-700 rounded">
      <div class="font-semibold mb-1">Failed to load trial balance</div>
      <div class="text-sm wrap-break-word">{{ error }}</div>
    </div>

    <div v-if="loading && !error" class="text-gray-600 dark:text-slate-300">Loading...</div>

    <div v-else class="border rounded overflow-x-auto dark:border-slate-800">
      <table class="min-w-full text-sm">
        <thead>
          <tr class="bg-gray-50 dark:bg-slate-900">
            <th class="p-2 text-left text-gray-700 dark:text-slate-200">Code</th>
            <th class="p-2 text-left text-gray-700 dark:text-slate-200">Account</th>
            <th class="p-2 text-right text-gray-700 dark:text-slate-200">Debit</th>
            <th class="p-2 text-right text-gray-700 dark:text-slate-200">Credit</th>
          </tr>
        </thead>

        <tbody>
          <tr v-for="(r, idx) in rows" :key="r.accountId ?? r.id ?? idx" class="border-t border-gray-200 dark:border-slate-800">
            <td class="p-2 whitespace-nowrap">{{ r.code ?? "—" }}</td>
            <td class="p-2">{{ r.name ?? "—" }}</td>
            <td class="p-2 text-right tabular-nums">{{ money(r.debit) }}</td>
            <td class="p-2 text-right tabular-nums">{{ money(r.credit) }}</td>
          </tr>

          <tr v-if="rows.length === 0" class="border-t border-gray-200 dark:border-slate-800">
            <td class="p-3 text-gray-500 dark:text-slate-300" colspan="4">No rows returned.</td>
          </tr>
        </tbody>

        <tfoot>
          <tr class="border-t border-gray-200 dark:border-slate-800 bg-gray-50 dark:bg-slate-900">
            <td class="p-2 font-semibold text-gray-700 dark:text-slate-200" colspan="2">Total</td>
            <td class="p-2 text-right font-semibold tabular-nums text-gray-700 dark:text-slate-200">{{ money(totalDebit) }}</td>
            <td class="p-2 text-right font-semibold tabular-nums text-gray-700 dark:text-slate-200">{{ money(totalCredit) }}</td>
          </tr>
        </tfoot>
      </table>
    </div>

    <div v-if="!loading && !error" class="text-sm text-gray-500 dark:text-slate-300">
      Balance check:
      <b :class="Math.abs(totalDebit - totalCredit) < 0.0001 ? 'text-green-600 dark:text-green-400' : 'text-red-600 dark:text-red-400'">
        {{ Math.abs(totalDebit - totalCredit) < 0.0001 ? "OK" : "NOT BALANCED" }}
      </b>
    </div>
  </div>
</template>
