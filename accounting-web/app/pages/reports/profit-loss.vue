<script setup lang="ts">
import { apiFetch } from "../../utils/api";

type Row = { code?: string | null; name?: string | null; amount?: number };
type Section = { title: string; rows: Row[]; total: number };

const loading = ref(true);
const error = ref<string | null>(null);

const income = ref<Section>({ title: "Income", rows: [], total: 0 });
const expense = ref<Section>({ title: "Expenses", rows: [], total: 0 });

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

async function load() {
  loading.value = true;
  error.value = null;
  try {
    // backend expected: GET /api/reports/profit-loss
    // expected shape: { income:{rows,total}, expense:{rows,total}, netProfit }
    const res = await apiFetch<any>(`/api/reports/profit-loss`);

    income.value = { title: "Income", rows: res?.income?.rows ?? [], total: n(res?.income?.total) };
    expense.value = { title: "Expenses", rows: res?.expense?.rows ?? [], total: n(res?.expense?.total) };
  } catch (e: any) {
    error.value = normalizeErr(e);
  } finally {
    loading.value = false;
  }
}

onMounted(load);

const netProfit = computed(() => income.value.total - expense.value.total);
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-start justify-between gap-3">
      <div>
        <h1 class="text-2xl font-bold">Profit & Loss</h1>
        <p class="text-sm text-gray-500 dark:text-slate-300">Posted journals only.</p>
      </div>

      <button class="px-3 py-2 rounded border text-sm border-gray-200 hover:bg-gray-50 dark:border-slate-800 dark:hover:bg-slate-900" @click="load" :disabled="loading">
        {{ loading ? "Loading..." : "Refresh" }}
      </button>
    </div>

    <div v-if="error" class="p-3 border border-red-200 bg-red-50 text-red-700 rounded">
      {{ error }}
    </div>

    <div v-if="loading" class="text-gray-600 dark:text-slate-300">Loading...</div>

    <div v-else class="grid lg:grid-cols-2 gap-4">
      <div class="border rounded overflow-x-auto dark:border-slate-800">
        <div class="p-3 border-b bg-gray-50 dark:bg-slate-900 dark:border-slate-800">
          <div class="font-semibold text-gray-700 dark:text-slate-200">Income</div>
        </div>
        <table class="min-w-full text-sm">
          <thead>
            <tr class="bg-gray-50 dark:bg-slate-900">
              <th class="p-2 text-left text-gray-700 dark:text-slate-200">Account</th>
              <th class="p-2 text-right text-gray-700 dark:text-slate-200">Amount</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(r, i) in income.rows" :key="i" class="border-t border-gray-200 dark:border-slate-800">
              <td class="p-2">{{ (r.code ? r.code + " - " : "") + (r.name ?? "—") }}</td>
              <td class="p-2 text-right tabular-nums">{{ money(r.amount) }}</td>
            </tr>
            <tr v-if="income.rows.length === 0" class="border-t border-gray-200 dark:border-slate-800">
              <td class="p-3 text-gray-500 dark:text-slate-300" colspan="2">No rows.</td>
            </tr>
          </tbody>
          <tfoot>
            <tr class="border-t border-gray-200 dark:border-slate-800 bg-gray-50 dark:bg-slate-900">
              <td class="p-2 font-semibold text-gray-700 dark:text-slate-200">Total Income</td>
              <td class="p-2 text-right font-semibold tabular-nums text-gray-700 dark:text-slate-200">{{ money(income.total) }}</td>
            </tr>
          </tfoot>
        </table>
      </div>

      <div class="border rounded overflow-x-auto dark:border-slate-800">
        <div class="p-3 border-b bg-gray-50 dark:bg-slate-900 dark:border-slate-800">
          <div class="font-semibold text-gray-700 dark:text-slate-200">Expenses</div>
        </div>
        <table class="min-w-full text-sm">
          <thead>
            <tr class="bg-gray-50 dark:bg-slate-900">
              <th class="p-2 text-left text-gray-700 dark:text-slate-200">Account</th>
              <th class="p-2 text-right text-gray-700 dark:text-slate-200">Amount</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(r, i) in expense.rows" :key="i" class="border-t border-gray-200 dark:border-slate-800">
              <td class="p-2">{{ (r.code ? r.code + " - " : "") + (r.name ?? "—") }}</td>
              <td class="p-2 text-right tabular-nums">{{ money(r.amount) }}</td>
            </tr>
            <tr v-if="expense.rows.length === 0" class="border-t border-gray-200 dark:border-slate-800">
              <td class="p-3 text-gray-500 dark:text-slate-300" colspan="2">No rows.</td>
            </tr>
          </tbody>
          <tfoot>
            <tr class="border-t border-gray-200 dark:border-slate-800 bg-gray-50 dark:bg-slate-900">
              <td class="p-2 font-semibold text-gray-700 dark:text-slate-200">Total Expenses</td>
              <td class="p-2 text-right font-semibold tabular-nums text-gray-700 dark:text-slate-200">{{ money(expense.total) }}</td>
            </tr>
          </tfoot>
        </table>
      </div>
    </div>

    <div class="border rounded p-4 dark:border-slate-800">
      <div class="text-sm text-gray-500 dark:text-slate-300">Net Profit</div>
      <div class="text-2xl font-bold tabular-nums" :class="netProfit >= 0 ? 'text-green-700 dark:text-green-300' : 'text-red-700 dark:text-red-300'">
        {{ money(netProfit) }}
      </div>
    </div>
  </div>
</template>
