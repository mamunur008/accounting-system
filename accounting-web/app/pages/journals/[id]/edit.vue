<script setup lang="ts">
const route = useRoute();
const router = useRouter();
const config = useRuntimeConfig();

const id = Number(route.params.id);

function apiUrl(path: string) {
  const base = String(config.public.apiBase || "").replace(/\/+$/, "");
  const p = path.startsWith("/") ? path : `/${path}`;
  return `${base}${p}`;
}
async function apiFetch<T>(path: string, opts: any = {}) {
  return await $fetch<T>(apiUrl(path), opts);
}

type Line = { id: number; accountId: number; debit: number; credit: number; memo?: string | null };
type Journal = {
  id: number;
  entryDate: string;
  reference?: string | null;
  description?: string | null;
  status: number | string;
  lines: Line[];
};

const loading = ref(true);
const saving = ref(false);
const error = ref<string | null>(null);
const journal = ref<Journal | null>(null);

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

const isDraft = computed(() => journal.value && normalizeStatus(journal.value.status) === "draft");

async function load() {
  loading.value = true;
  error.value = null;
  try {
    journal.value = await apiFetch<Journal>(`/api/journals/${id}`);
    if (journal.value && normalizeStatus(journal.value.status) !== "draft") {
      error.value = "Only Draft journals can be edited.";
    }
  } catch (e: any) {
    error.value = normalizeErr(e);
  } finally {
    loading.value = false;
  }
}

// NOTE: This assumes you have a PUT endpoint. If not, keep this page read-only or add the endpoint.
// backend expected: PUT /api/journals/{id}
async function save() {
  if (!journal.value) return;
  if (!isDraft.value) return;
  saving.value = true;
  error.value = null;
  try {
    await apiFetch(`/api/journals/${journal.value.id}`, { method: "PUT", body: journal.value });
    await router.push(`/journals/${journal.value.id}`);
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
        <h1 class="text-2xl font-bold">Edit Journal #{{ id }}</h1>
        <p class="text-sm text-gray-500 dark:text-slate-300">Draft only. If you don’t have PUT in backend, keep this page as read-only.</p>
      </div>
      <div class="flex gap-2">
        <NuxtLink :to="`/journals/${id}`" class="px-3 py-2 rounded border text-sm border-gray-200 hover:bg-gray-50 dark:border-slate-800 dark:hover:bg-slate-900"> Back </NuxtLink>
        <button class="px-4 py-2 rounded bg-black text-white text-sm hover:bg-gray-900 dark:bg-white dark:text-black dark:hover:bg-gray-100" @click="save" :disabled="saving || !isDraft">
          {{ saving ? "Saving..." : "Save" }}
        </button>
      </div>
    </div>

    <div v-if="error" class="p-3 border border-red-200 bg-red-50 text-red-700 rounded">
      {{ error }}
    </div>

    <div v-if="loading" class="text-gray-600 dark:text-slate-300">Loading...</div>

    <div v-else-if="journal" class="border rounded p-4 space-y-4 dark:border-slate-800">
      <div class="grid md:grid-cols-3 gap-3">
        <div>
          <label class="block text-xs text-gray-500 dark:text-slate-300">Entry Date</label>
          <input v-model="journal.entryDate" type="date" class="w-full px-3 py-2 rounded border text-sm border-gray-200 dark:border-slate-800 bg-white dark:bg-slate-950" :disabled="!isDraft" />
        </div>
        <div>
          <label class="block text-xs text-gray-500 dark:text-slate-300">Reference</label>
          <input v-model="journal.reference" type="text" class="w-full px-3 py-2 rounded border text-sm border-gray-200 dark:border-slate-800 bg-white dark:bg-slate-950" :disabled="!isDraft" />
        </div>
        <div>
          <label class="block text-xs text-gray-500 dark:text-slate-300">Description</label>
          <input v-model="journal.description" type="text" class="w-full px-3 py-2 rounded border text-sm border-gray-200 dark:border-slate-800 bg-white dark:bg-slate-950" :disabled="!isDraft" />
        </div>
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
              <td class="p-2">
                <input v-model.number="l.accountId" type="number" class="w-32 px-2 py-2 rounded border text-sm border-gray-200 dark:border-slate-800 bg-white dark:bg-slate-950" :disabled="!isDraft" />
              </td>
              <td class="p-2 text-right">
                <input v-model.number="l.debit" type="number" step="0.01" class="w-32 px-2 py-2 rounded border text-sm text-right border-gray-200 dark:border-slate-800 bg-white dark:bg-slate-950" :disabled="!isDraft" />
              </td>
              <td class="p-2 text-right">
                <input v-model.number="l.credit" type="number" step="0.01" class="w-32 px-2 py-2 rounded border text-sm text-right border-gray-200 dark:border-slate-800 bg-white dark:bg-slate-950" :disabled="!isDraft" />
              </td>
              <td class="p-2">
                <input v-model="l.memo" type="text" class="w-full px-2 py-2 rounded border text-sm border-gray-200 dark:border-slate-800 bg-white dark:bg-slate-950" :disabled="!isDraft" />
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <p class="text-xs text-gray-500 dark:text-slate-300">If you want a real edit experience, we’ll switch AccountId → account dropdown like New Journal.</p>
    </div>
  </div>
</template>
