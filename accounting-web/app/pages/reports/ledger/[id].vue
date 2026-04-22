<script setup lang="ts">
import { apiFetch } from "../../../utils/api";

const route = useRoute();
const accountId = Number(route.params.id);

type LedgerRow = {
  journalId: number;
  entryDate: string;
  reference?: string | null;
  description?: string | null;
  debit: number;
  credit: number;
  memo?: string | null;
};

const loading = ref(true);
const error = ref<string | null>(null);
const rows = ref<LedgerRow[]>([]);

function normalizeErr(e: any) {
  const msg = e?.data?.message ?? e?.data ?? e?.message ?? "Request failed";
  return typeof msg === "string" ? msg : JSON.stringify(msg);
}

const totalDebit = computed(() => rows.value.reduce((s, r) => s + Number(r.debit || 0), 0));
const totalCredit = computed(() => rows.value.reduce((s, r) => s + Number(r.credit || 0), 0));
const balance = computed(() => totalDebit.value - totalCredit.value);

async function load() {
  loading.value = true;
  error.value = null;
  try {
    // backend expected: GET /api/ledger/{accountId}
    rows.value = await apiFetch<LedgerRow[]>(`/api/ledger/${accountId}`);
  } catch (e: any) {
    error.value = normalizeErr(e);
    rows.value = [];
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
        <h1 class="text-2xl font-bold">Ledger</h1>
        <p class="text-sm text-gray-500 dark:text-slate-300">
          AccountId: <span class="font-mono">{{ accountId }}</span>
        </p>
      </div>

      <button class="px-3 py-2 rounded border text-sm border-gray-200 hover:bg-gray-50 dark:border-slate-800 dark:hover:bg-slate-900" @click="load" :disabled="loading">
        {{ loading ? "Loading..." : "Refresh" }}
      </button>
    </div>

    <div v-if="error" class="p-3 border border-red-200 bg-red-50 text-red-700 rounded">
      {{ error }}
    </div>

    <div class="border rounded overflow-x-auto dark:border-slate-800">
      <table class="min-w-full text-sm">
        <thead>
          <tr class="bg-gray-50 dark:bg-slate-900">
            <th class="p-2 text-left text-gray-700 dark:text-slate-200">Date</th>
            <th class="p-2 text-left text-gray-700 dark:text-slate-200">Journal</th>
            <th class="p-2 text-left text-gray-700 dark:text-slate-200">Ref</th>
            <th class="p-2 text-left text-gray-700 dark:text-slate-200">Memo</th>
            <th class="p-2 text-right text-gray-700 dark:text-slate-200">Debit</th>
            <th class="p-2 text-right text-gray-700 dark:text-slate-200">Credit</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(r, i) in rows" :key="i" class="border-t border-gray-200 dark:border-slate-800">
            <td class="p-2">{{ r.entryDate }}</td>
            <td class="p-2">
              <NuxtLink :to="`/journals/${r.journalId}`" class="text-sm underline text-gray-700 dark:text-slate-200"> #{{ r.journalId }} </NuxtLink>
            </td>
            <td class="p-2">{{ r.reference || "—" }}</td>
            <td class="p-2">{{ r.memo || "—" }}</td>
            <td class="p-2 text-right tabular-nums">{{ Number(r.debit).toFixed(2) }}</td>
            <td class="p-2 text-right tabular-nums">{{ Number(r.credit).toFixed(2) }}</td>
          </tr>

          <tr v-if="!loading && rows.length === 0" class="border-t border-gray-200 dark:border-slate-800">
            <td class="p-3 text-gray-500 dark:text-slate-300" colspan="6">No ledger rows.</td>
          </tr>
        </tbody>
        <tfoot>
          <tr class="border-t border-gray-200 dark:border-slate-800 bg-gray-50 dark:bg-slate-900">
            <td class="p-2 font-semibold text-gray-700 dark:text-slate-200" colspan="4">Totals</td>
            <td class="p-2 text-right font-semibold tabular-nums text-gray-700 dark:text-slate-200">
              {{ totalDebit.toFixed(2) }}
            </td>
            <td class="p-2 text-right font-semibold tabular-nums text-gray-700 dark:text-slate-200">
              {{ totalCredit.toFixed(2) }}
            </td>
          </tr>
          <tr class="bg-gray-50 dark:bg-slate-900">
            <td class="p-2 text-xs text-gray-500 dark:text-slate-300" colspan="6">
              Balance (Debit - Credit): <b class="tabular-nums">{{ balance.toFixed(2) }}</b>
            </td>
          </tr>
        </tfoot>
      </table>
    </div>
  </div>
</template>
