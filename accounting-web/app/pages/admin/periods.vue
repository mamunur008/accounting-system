<script setup lang="ts">
import { ref, onMounted } from "vue";
import { apiFetch } from "../../utils/api";

type PeriodRow = {
  id: number;
  year: number;
  month: number;
  key: string; // "YYYY-MM"
  isClosed: boolean;
  closedAt?: string | null;
  closedBy?: string | null;
};

const year = ref(new Date().getFullYear());
const loading = ref(false);
const error = ref<string | null>(null);
const rows = ref<PeriodRow[]>([]);

// track what we are doing so the button label is correct even with optimistic UI
const busy = ref<{ id: number; action: "close" | "open" } | null>(null);

function ym(y: number, m: number) {
  return `${y}-${String(m).padStart(2, "0")}`;
}

function monthName(m: number) {
  return new Date(2000, m - 1, 1).toLocaleString(undefined, { month: "long" });
}

function normalizeErr(e: any) {
  const msg = e?.data?.message ?? e?.data ?? e?.message ?? "Request failed";
  return typeof msg === "string" ? msg : JSON.stringify(msg);
}

// .NET returns PascalCase: Id, Year, Month, IsClosed, ClosedAt, ClosedBy
function normalizeRow(r: any): PeriodRow {
  const y = Number(r.Year ?? r.year);
  const m = Number(r.Month ?? r.month);

  return {
    id: Number(r.Id ?? r.id),
    year: y,
    month: m,
    key: String(r.key ?? ym(y, m)),
    isClosed: Boolean(r.IsClosed ?? r.isClosed ?? r.is_closed),
    closedAt: (r.ClosedAt ?? r.closedAt ?? r.closed_at ?? null) as string | null,
    closedBy: (r.ClosedBy ?? r.closedBy ?? r.closed_by ?? null) as string | null,
  };
}

async function load() {
  loading.value = true;
  error.value = null;
  try {
    const data = await apiFetch<any[]>(`/api/periods?year=${year.value}`);
    rows.value = (data ?? []).map(normalizeRow);
  } catch (e: any) {
    error.value = normalizeErr(e);
    rows.value = [];
  } finally {
    loading.value = false;
  }
}

async function setClosed(r: PeriodRow, shouldClose: boolean) {
  error.value = null;
  busy.value = { id: r.id, action: shouldClose ? "close" : "open" };

  // optimistic UI update so it changes immediately
  rows.value = rows.value.map((x) =>
    x.id === r.id
      ? {
          ...x,
          isClosed: shouldClose,
          closedAt: shouldClose ? (x.closedAt ?? new Date().toISOString()) : null,
          closedBy: shouldClose ? (x.closedBy ?? "system") : null,
        }
      : x,
  );

  try {
    const updated = await apiFetch<any>(`/api/periods/${r.id}/toggle`, {
      method: "POST",
      body: { isClosed: shouldClose }, // model binder is case-insensitive
    });

    const u = normalizeRow(updated);
    rows.value = rows.value.map((x) => (x.id === u.id ? u : x));
  } catch (e: any) {
    // revert if it fails
    rows.value = rows.value.map((x) => (x.id === r.id ? { ...x, isClosed: !shouldClose } : x));
    error.value = normalizeErr(e);
  } finally {
    busy.value = null;
  }
}

async function closeMonth(r: PeriodRow) {
  await setClosed(r, true);
}

async function openMonth(r: PeriodRow) {
  await setClosed(r, false);
}

onMounted(load);
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-start justify-between gap-3">
      <div>
        <h1 class="text-2xl font-bold">Accounting Periods</h1>
        <p class="text-sm text-gray-500 dark:text-slate-300">Close a month to prevent posting journals dated in that month.</p>
      </div>

      <NuxtLink to="/journals" class="px-3 py-2 rounded border text-sm border-gray-200 hover:bg-gray-50 dark:border-slate-800 dark:hover:bg-slate-900"> Back to Journals </NuxtLink>
    </div>

    <div class="flex items-end gap-2">
      <div>
        <label class="block text-xs text-gray-500 dark:text-slate-300">Year</label>
        <input v-model.number="year" type="number" class="w-40 px-3 py-2 rounded border text-sm border-gray-200 dark:border-slate-800 bg-white dark:bg-slate-950" />
      </div>

      <button class="px-3 py-2 rounded bg-black text-white text-sm hover:bg-gray-900 dark:bg-white dark:text-black dark:hover:bg-gray-100" @click="load" :disabled="loading">
        {{ loading ? "Loading..." : "Load" }}
      </button>
    </div>

    <div v-if="error" class="p-3 border border-red-200 bg-red-50 text-red-700 rounded">
      {{ error }}
    </div>

    <div class="border rounded overflow-x-auto dark:border-slate-800">
      <table class="min-w-full text-sm">
        <thead>
          <tr class="bg-gray-50 dark:bg-slate-900">
            <th class="p-2 text-left text-gray-700 dark:text-slate-200">Month</th>
            <th class="p-2 text-left text-gray-700 dark:text-slate-200">Status</th>
            <th class="p-2 text-left text-gray-700 dark:text-slate-200">Closed Info</th>
            <th class="p-2 text-right text-gray-700 dark:text-slate-200">Action</th>
          </tr>
        </thead>

        <tbody>
          <tr v-for="r in rows" :key="r.id" class="border-t border-gray-200 dark:border-slate-800">
            <td class="p-2 font-medium">{{ monthName(r.month) }} ({{ r.key }})</td>

            <td class="p-2">
              <span class="text-xs px-2 py-0.5 rounded" :class="r.isClosed ? 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-200' : 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-200'">
                {{ r.isClosed ? "Closed" : "Open" }}
              </span>
            </td>

            <td class="p-2 text-xs text-gray-500 dark:text-slate-300">
              <span v-if="r.isClosed && r.closedAt"> {{ r.closedAt }} by {{ r.closedBy || "system" }} </span>
              <span v-else>—</span>
            </td>

            <td class="p-2 text-right">
              <button v-if="r.isClosed" class="px-3 py-1.5 rounded bg-emerald-600 text-white text-xs hover:bg-emerald-700 disabled:opacity-60" :disabled="busy?.id === r.id" @click="openMonth(r)">
                {{ busy?.id === r.id && busy?.action === "open" ? "Opening..." : "Open" }}
              </button>

              <button v-else class="px-3 py-1.5 rounded bg-red-600 text-white text-xs hover:bg-red-700 disabled:opacity-60" :disabled="busy?.id === r.id" @click="closeMonth(r)">
                {{ busy?.id === r.id && busy?.action === "close" ? "Closing..." : "Close" }}
              </button>
            </td>
          </tr>

          <tr v-if="!loading && rows.length === 0" class="border-t border-gray-200 dark:border-slate-800">
            <td class="p-3 text-gray-500 dark:text-slate-300" colspan="4">No rows returned.</td>
          </tr>
        </tbody>
      </table>
    </div>

    <p class="text-xs text-gray-500 dark:text-slate-300">Test: Close a month, then try posting a journal dated in that month — posting should be blocked.</p>
  </div>
</template>
