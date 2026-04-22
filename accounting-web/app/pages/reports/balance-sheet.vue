<script setup lang="ts">
import { apiFetch } from "../../utils/api";

type Row = { code?: string | null; name?: string | null; amount?: number };
type Section = { title: string; rows: Row[]; total: number };

const loading = ref(true);
const error = ref<string | null>(null);

const assets = ref<Section>({ title: "Assets", rows: [], total: 0 });
const liabilities = ref<Section>({ title: "Liabilities", rows: [], total: 0 });
const equity = ref<Section>({ title: "Equity", rows: [], total: 0 });

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
    // backend expected: GET /api/reports/balance-sheet
    // expected shape example:
    // { assets:{rows:[...],total:...}, liabilities:{rows:[...],total:...}, equity:{rows:[...],total:...} }
    const res = await apiFetch<any>(`/api/reports/balance-sheet`);

    assets.value = { title: "Assets", rows: res?.assets?.rows ?? [], total: n(res?.assets?.total) };
    liabilities.value = { title: "Liabilities", rows: res?.liabilities?.rows ?? [], total: n(res?.liabilities?.total) };
    equity.value = { title: "Equity", rows: res?.equity?.rows ?? [], total: n(res?.equity?.total) };
  } catch (e: any) {
    error.value = normalizeErr(e);
  } finally {
    loading.value = false;
  }
}

onMounted(load);

const rightTotal = computed(() => liabilities.value.total + equity.value.total);
const balanced = computed(() => Math.abs(assets.value.total - rightTotal.value) < 0.0001);
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-start justify-between gap-3">
      <div>
        <h1 class="text-2xl font-bold">Balance Sheet</h1>
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
      <!-- Assets -->
      <div class="border rounded overflow-x-auto dark:border-slate-800">
        <div class="p-3 border-b bg-gray-50 dark:bg-slate-900 dark:border-slate-800">
          <div class="font-semibold text-gray-700 dark:text-slate-200">Assets</div>
        </div>
        <table class="min-w-full text-sm">
          <thead>
            <tr class="bg-gray-50 dark:bg-slate-900">
              <th class="p-2 text-left text-gray-700 dark:text-slate-200">Account</th>
              <th class="p-2 text-right text-gray-700 dark:text-slate-200">Amount</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(r, i) in assets.rows" :key="i" class="border-t border-gray-200 dark:border-slate-800">
              <td class="p-2">{{ (r.code ? r.code + " - " : "") + (r.name ?? "—") }}</td>
              <td class="p-2 text-right tabular-nums">{{ money(r.amount) }}</td>
            </tr>
            <tr v-if="assets.rows.length === 0" class="border-t border-gray-200 dark:border-slate-800">
              <td class="p-3 text-gray-500 dark:text-slate-300" colspan="2">No rows.</td>
            </tr>
          </tbody>
          <tfoot>
            <tr class="border-t border-gray-200 dark:border-slate-800 bg-gray-50 dark:bg-slate-900">
              <td class="p-2 font-semibold text-gray-700 dark:text-slate-200">Total Assets</td>
              <td class="p-2 text-right font-semibold tabular-nums text-gray-700 dark:text-slate-200">{{ money(assets.total) }}</td>
            </tr>
          </tfoot>
        </table>
      </div>

      <!-- Liabilities + Equity -->
      <div class="space-y-4">
        <div class="border rounded overflow-x-auto dark:border-slate-800">
          <div class="p-3 border-b bg-gray-50 dark:bg-slate-900 dark:border-slate-800">
            <div class="font-semibold text-gray-700 dark:text-slate-200">Liabilities</div>
          </div>
          <table class="min-w-full text-sm">
            <thead>
              <tr class="bg-gray-50 dark:bg-slate-900">
                <th class="p-2 text-left text-gray-700 dark:text-slate-200">Account</th>
                <th class="p-2 text-right text-gray-700 dark:text-slate-200">Amount</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(r, i) in liabilities.rows" :key="i" class="border-t border-gray-200 dark:border-slate-800">
                <td class="p-2">{{ (r.code ? r.code + " - " : "") + (r.name ?? "—") }}</td>
                <td class="p-2 text-right tabular-nums">{{ money(r.amount) }}</td>
              </tr>
              <tr v-if="liabilities.rows.length === 0" class="border-t border-gray-200 dark:border-slate-800">
                <td class="p-3 text-gray-500 dark:text-slate-300" colspan="2">No rows.</td>
              </tr>
            </tbody>
            <tfoot>
              <tr class="border-t border-gray-200 dark:border-slate-800 bg-gray-50 dark:bg-slate-900">
                <td class="p-2 font-semibold text-gray-700 dark:text-slate-200">Total Liabilities</td>
                <td class="p-2 text-right font-semibold tabular-nums text-gray-700 dark:text-slate-200">{{ money(liabilities.total) }}</td>
              </tr>
            </tfoot>
          </table>
        </div>

        <div class="border rounded overflow-x-auto dark:border-slate-800">
          <div class="p-3 border-b bg-gray-50 dark:bg-slate-900 dark:border-slate-800">
            <div class="font-semibold text-gray-700 dark:text-slate-200">Equity</div>
          </div>
          <table class="min-w-full text-sm">
            <thead>
              <tr class="bg-gray-50 dark:bg-slate-900">
                <th class="p-2 text-left text-gray-700 dark:text-slate-200">Account</th>
                <th class="p-2 text-right text-gray-700 dark:text-slate-200">Amount</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(r, i) in equity.rows" :key="i" class="border-t border-gray-200 dark:border-slate-800">
                <td class="p-2">{{ (r.code ? r.code + " - " : "") + (r.name ?? "—") }}</td>
                <td class="p-2 text-right tabular-nums">{{ money(r.amount) }}</td>
              </tr>
              <tr v-if="equity.rows.length === 0" class="border-t border-gray-200 dark:border-slate-800">
                <td class="p-3 text-gray-500 dark:text-slate-300" colspan="2">No rows.</td>
              </tr>
            </tbody>
            <tfoot>
              <tr class="border-t border-gray-200 dark:border-slate-800 bg-gray-50 dark:bg-slate-900">
                <td class="p-2 font-semibold text-gray-700 dark:text-slate-200">Total Equity</td>
                <td class="p-2 text-right font-semibold tabular-nums text-gray-700 dark:text-slate-200">{{ money(equity.total) }}</td>
              </tr>
            </tfoot>
          </table>
        </div>

        <div class="text-sm text-gray-500 dark:text-slate-300">
          Balance check:
          <b :class="balanced ? 'text-green-600 dark:text-green-400' : 'text-red-600 dark:text-red-400'">
            {{ balanced ? "OK" : "NOT BALANCED" }}
          </b>
          <span class="ml-2">(Assets: {{ money(assets.total) }} | Liab+Equity: {{ money(rightTotal) }})</span>
        </div>
      </div>
    </div>
  </div>
</template>
