<script setup lang="ts">
import { apiFetch } from "../../utils/api";

type JournalRow = {
  id: number;
  entryDate: string;
  reference?: string | null;
  description?: string | null;
  status: number | string;
  postedAt?: string | null;
  postedBy?: string | null;
  deletedAt?: string | null;
  deletedBy?: string | null;
  number?: string | null;
};

const loading = ref(true);
const error = ref<string | null>(null);
const list = ref<JournalRow[]>([]);

function normalizeErr(e: any) {
  const msg = e?.data?.message ?? e?.data ?? e?.message ?? "Request failed";
  return typeof msg === "string" ? msg : JSON.stringify(msg);
}

function normalizeStatus(s: any): "draft" | "posted" | "deleted" | "unknown" {
  if (typeof s === "number") return s === 0 ? "draft" : s === 1 ? "posted" : s === 2 ? "deleted" : "unknown";
  if (typeof s === "string") {
    const t = s.trim().toLowerCase();
    if (t.includes("draft")) return "draft";
    if (t.includes("posted")) return "posted";
    if (t.includes("deleted")) return "deleted";
  }
  return "unknown";
}

function badgeClass(st: ReturnType<typeof normalizeStatus>) {
  if (st === "draft") return "bg-gray-100 text-gray-700 dark:bg-slate-800 dark:text-slate-200";
  if (st === "posted") return "bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-200";
  if (st === "deleted") return "bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-200";
  return "bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-200";
}

async function load() {
  loading.value = true;
  error.value = null;
  try {
    list.value = await apiFetch<JournalRow[]>(`/api/journals`);
  } catch (e: any) {
    error.value = normalizeErr(e);
    list.value = [];
  } finally {
    loading.value = false;
  }
}

async function post(id: number) {
  error.value = null;
  try {
    await apiFetch(`/api/journals/${id}/post`, { method: "POST" });
    await load();
  } catch (e: any) {
    error.value = normalizeErr(e);
  }
}

async function del(id: number) {
  if (!confirm("Delete this journal?")) return;
  error.value = null;
  try {
    await apiFetch(`/api/journals/${id}/delete`, { method: "POST" });
    await load();
  } catch (e: any) {
    error.value = normalizeErr(e);
  }
}

onMounted(load);
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-start justify-between gap-3">
      <div>
        <h1 class="text-2xl font-bold">Journals</h1>
        <p class="text-sm text-gray-500 dark:text-slate-300">Draft → Post (locks). Draft → Delete (soft).</p>
      </div>

      <div class="flex gap-2">
        <NuxtLink to="/journals/new" class="px-3 py-2 rounded bg-black text-white text-sm hover:bg-gray-900 dark:bg-white dark:text-black dark:hover:bg-gray-100"> + New Journal </NuxtLink>

        <NuxtLink to="/reports/trial-balance" class="px-3 py-2 rounded border text-sm border-gray-200 hover:bg-gray-50 dark:border-slate-800 dark:hover:bg-slate-900"> Trial Balance </NuxtLink>
      </div>
    </div>

    <div v-if="error" class="p-3 border border-red-200 bg-red-50 text-red-700 rounded">
      {{ error }}
    </div>

    <div class="border rounded overflow-x-auto dark:border-slate-800">
      <table class="min-w-full text-sm">
        <thead>
          <tr class="bg-gray-50 dark:bg-slate-900">
            <th class="p-2 text-left text-gray-700 dark:text-slate-200">ID</th>
            <th class="p-2 text-left text-gray-700 dark:text-slate-200">Date</th>
            <th class="p-2 text-left text-gray-700 dark:text-slate-200">Ref</th>
            <th class="p-2 text-left text-gray-700 dark:text-slate-200">Description</th>
            <th class="p-2 text-left text-gray-700 dark:text-slate-200">Status</th>
            <th class="p-2 text-left text-gray-700 dark:text-slate-200">Number</th>
            <th class="p-2 text-left text-gray-700 dark:text-slate-200">Audit</th>
            <th class="p-2 text-right text-gray-700 dark:text-slate-200">Actions</th>
          </tr>
        </thead>

        <tbody>
          <tr v-for="j in list" :key="j.id" class="border-t border-gray-200 dark:border-slate-800">
            <td class="p-2 font-mono">{{ j.id }}</td>
            <td class="p-2">{{ j.entryDate }}</td>
            <td class="p-2">{{ j.reference || "—" }}</td>
            <td class="p-2">{{ j.description || "—" }}</td>
            <td class="p-2">
              <span class="text-xs px-2 py-0.5 rounded" :class="badgeClass(normalizeStatus(j.status))">
                {{ normalizeStatus(j.status) }}
              </span>
            </td>
            <td class="p-2 text-xs text-gray-600 dark:text-slate-300">
              {{ j.number || "—" }}
            </td>
            <td class="p-2 text-xs text-gray-600 dark:text-slate-300">
              <span v-if="j.postedAt">Posted: {{ j.postedAt }} by {{ j.postedBy || "system" }}</span>
              <span v-else-if="j.deletedAt">Deleted: {{ j.deletedAt }} by {{ j.deletedBy || "system" }}</span>
              <span v-else>—</span>
            </td>
            <td class="p-2 text-right">
              <div class="inline-flex gap-2">
                <button v-if="normalizeStatus(j.status) === 'draft'" class="px-3 py-1.5 rounded bg-emerald-600 text-white text-xs hover:bg-emerald-700" @click="post(j.id)">Post</button>
                <button v-if="normalizeStatus(j.status) === 'draft'" class="px-3 py-1.5 rounded bg-red-600 text-white text-xs hover:bg-red-700" @click="del(j.id)">Delete</button>

                <NuxtLink :to="`/journals/${j.id}`" class="px-3 py-1.5 rounded border text-xs border-gray-200 hover:bg-gray-50 dark:border-slate-800 dark:hover:bg-slate-900"> View </NuxtLink>
              </div>
            </td>
          </tr>

          <tr v-if="!loading && list.length === 0" class="border-t border-gray-200 dark:border-slate-800">
            <td class="p-3 text-gray-500 dark:text-slate-300" colspan="8">No journals found.</td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-if="loading" class="text-sm text-gray-500 dark:text-slate-300">Loading...</div>
  </div>
</template>
