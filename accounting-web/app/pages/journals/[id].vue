<script setup lang="ts">
const route = useRoute();
const router = useRouter();
const config = useRuntimeConfig();

const id = Number(route.params.id);

// ---- direct API helper (NO IMPORTS, NO PROXY) ----
function apiUrl(path: string) {
  const base = String(config.public.apiBase || "").replace(/\/+$/, "");
  const p = path.startsWith("/") ? path : `/${path}`;
  return `${base}${p}`;
}
async function apiFetch<T>(path: string, opts: any = {}) {
  return await $fetch<T>(apiUrl(path), opts);
}

// ---- types ----
type Line = { id: number; accountId: number; debit: number; credit: number; memo?: string | null };
type Journal = {
  id: number;
  entryDate: string;
  reference?: string | null;
  description?: string | null;
  status: number | string;
  postedAt?: string | null;
  postedBy?: string | null;
  deletedAt?: string | null;
  deletedBy?: string | null;
  lines: Line[];
};

const loading = ref(true);
const error = ref<string | null>(null);
const journal = ref<Journal | null>(null);

function normalizeStatus(s: any): "draft" | "posted" | "deleted" | "unknown" {
  if (typeof s === "number") {
    if (s === 0) return "draft";
    if (s === 1) return "posted";
    if (s === 2) return "deleted";
    return "unknown";
  }
  if (typeof s === "string") {
    const t = s.trim().toLowerCase();
    if (t.includes("draft")) return "draft";
    if (t.includes("posted")) return "posted";
    if (t.includes("deleted")) return "deleted";
  }
  return "unknown";
}

const status = computed(() => (journal.value ? normalizeStatus(journal.value.status) : "unknown"));
const isDraft = computed(() => status.value === "draft");
const isPosted = computed(() => status.value === "posted");
const isDeleted = computed(() => status.value === "deleted");

function statusLabel() {
  if (status.value === "draft") return "Draft";
  if (status.value === "posted") return "Posted";
  if (status.value === "deleted") return "Deleted";
  return "Unknown";
}

function normalizeErr(e: any) {
  const msg = e?.data?.message ?? e?.data ?? e?.message ?? "Request failed";
  return typeof msg === "string" ? msg : JSON.stringify(msg);
}

async function load() {
  loading.value = true;
  error.value = null;
  try {
    journal.value = await apiFetch<Journal>(`/api/journals/${id}`);
  } catch (e: any) {
    error.value = normalizeErr(e);
  } finally {
    loading.value = false;
  }
}

async function post() {
  if (!journal.value) return;
  error.value = null;
  try {
    await apiFetch(`/api/journals/${journal.value.id}/post`, { method: "POST" });
    await load();
  } catch (e: any) {
    error.value = normalizeErr(e);
  }
}

async function softDelete() {
  if (!journal.value) return;
  if (!confirm("Delete this journal?")) return;
  error.value = null;
  try {
    await apiFetch(`/api/journals/${journal.value.id}/delete`, { method: "POST" });
    await load();
  } catch (e: any) {
    error.value = normalizeErr(e);
  }
}

async function reverse() {
  if (!journal.value) return;
  if (!confirm("Create a reversal journal (Draft) for this Posted entry?")) return;

  error.value = null;
  try {
    const res = await apiFetch<any>(`/api/journals/${journal.value.id}/reverse`, { method: "POST" });
    const newId = Number(res?.id ?? res?.Id);
    if (!newId || Number.isNaN(newId)) {
      throw new Error(`Reverse succeeded but response had no id. Response: ${JSON.stringify(res)}`);
    }
    await router.push(`/journals/${newId}`);
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
        <h1 class="text-2xl font-bold">Journal #{{ id }}</h1>
        <p v-if="journal" class="text-sm text-gray-500 dark:text-slate-300">
          Status: <b>{{ statusLabel() }}</b>
        </p>
      </div>

      <NuxtLink to="/journals" class="px-3 py-2 rounded border text-sm border-gray-200 hover:bg-gray-50 dark:border-slate-800 dark:hover:bg-slate-900"> Back </NuxtLink>
    </div>

    <div v-if="error" class="p-3 border border-red-200 bg-red-50 text-red-700 rounded">
      {{ error }}
    </div>

    <div v-if="loading" class="text-gray-600 dark:text-slate-300">Loading...</div>

    <template v-else-if="journal">
      <div class="border rounded p-4 space-y-2 dark:border-slate-800">
        <div><b>Date:</b> {{ journal.entryDate }}</div>
        <div><b>Reference:</b> {{ journal.reference || "—" }}</div>
        <div><b>Description:</b> {{ journal.description || "—" }}</div>

        <div v-if="journal.postedAt" class="text-sm text-gray-500 dark:text-slate-300">Posted: {{ journal.postedAt }} by {{ journal.postedBy || "—" }}</div>

        <div v-if="journal.deletedAt" class="text-sm text-gray-500 dark:text-slate-300">Deleted: {{ journal.deletedAt }} by {{ journal.deletedBy || "—" }}</div>
      </div>

      <div class="flex gap-2">
        <button v-if="isDraft" class="px-3 py-2 rounded bg-black text-white text-sm hover:bg-gray-900 dark:bg-white dark:text-black dark:hover:bg-gray-100" @click="post">Post</button>

        <button v-if="isDraft" class="px-3 py-2 rounded bg-red-600 text-white text-sm hover:bg-red-700" @click="softDelete">Delete</button>

        <button v-if="isPosted" class="px-3 py-2 rounded bg-indigo-600 text-white text-sm hover:bg-indigo-700" @click="reverse">Reverse</button>

        <span v-if="isDeleted" class="text-sm text-gray-500 dark:text-slate-300 self-center"> Deleted entries cannot be posted or reversed. </span>
      </div>

      <div class="border rounded overflow-x-auto dark:border-slate-800">
        <table class="min-w-full text-sm">
          <thead>
            <tr class="bg-gray-50 dark:bg-slate-900">
              <th class="p-2 text-left text-gray-700 dark:text-slate-200">AccountId</th>
              <th class="p-2 text-right text-gray-700 dark:text-slate-200">Debit</th>
              <th class="p-2 text-right text-gray-700 dark:text-slate-200">Credit</th>
              <th class="p-2 text-left text-gray-700 dark:text-slate-200">Memo</th>
            </tr>
          </thead>

          <tbody>
            <tr v-for="l in journal.lines" :key="l.id" class="border-t border-gray-200 dark:border-slate-800">
              <td class="p-2">{{ l.accountId }}</td>
              <td class="p-2 text-right tabular-nums">{{ Number(l.debit).toFixed(2) }}</td>
              <td class="p-2 text-right tabular-nums">{{ Number(l.credit).toFixed(2) }}</td>
              <td class="p-2">{{ l.memo || "—" }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </template>
  </div>
</template>
