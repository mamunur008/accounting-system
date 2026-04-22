<script setup lang="ts">
import { apiFetch } from "../../utils/api";

type Account = { id: number; code?: string | null; name: string; isPostable: boolean; isActive: boolean };
type Line = { accountId: number | null; debit: number; credit: number; memo: string };

const router = useRouter();

const loading = ref(false);
const error = ref<string | null>(null);

const entryDate = ref(new Date().toISOString().slice(0, 10));
const reference = ref("");
const description = ref("");

const accountsLoading = ref(true);
const accounts = ref<Account[]>([]);

const lines = ref<Line[]>([
  { accountId: null, debit: 0, credit: 0, memo: "" },
  { accountId: null, debit: 0, credit: 0, memo: "" },
]);

function normalizeErr(e: any) {
  const msg = e?.data?.message ?? e?.data ?? e?.message ?? "Request failed";
  return typeof msg === "string" ? msg : JSON.stringify(msg);
}

const totalDebit = computed(() => lines.value.reduce((s, l) => s + Number(l.debit || 0), 0));
const totalCredit = computed(() => lines.value.reduce((s, l) => s + Number(l.credit || 0), 0));
const balanced = computed(() => Math.abs(totalDebit.value - totalCredit.value) < 0.0001);

function addLine() {
  lines.value.push({ accountId: null, debit: 0, credit: 0, memo: "" });
}
function removeLine(i: number) {
  if (lines.value.length <= 2) return;
  lines.value.splice(i, 1);
}

function setDebit(i: number) {
  if (Number(lines.value[i].debit) > 0) lines.value[i].credit = 0;
}
function setCredit(i: number) {
  if (Number(lines.value[i].credit) > 0) lines.value[i].debit = 0;
}

async function loadAccounts() {
  accountsLoading.value = true;
  error.value = null;
  try {
    // backend should return flat list of accounts OR we can use /tree if that’s what you have
    // If you only have /api/accounts/tree, change this endpoint and flatten on frontend.
    accounts.value = await apiFetch<Account[]>(`/api/accounts`);
  } catch {
    // fallback: try tree endpoint if flat endpoint doesn't exist
    try {
      const tree = await apiFetch<any[]>(`/api/accounts/tree`);
      const out: Account[] = [];
      const walk = (nodes: any[]) => {
        for (const n of nodes) {
          out.push({ id: n.id, code: n.code, name: n.name, isPostable: !!n.isPostable, isActive: !!n.isActive });
          if (n.children?.length) walk(n.children);
        }
      };
      walk(tree);
      accounts.value = out.filter((a) => a.isActive && a.isPostable);
    } catch (e: any) {
      error.value = normalizeErr(e);
      accounts.value = [];
    }
  } finally {
    // Only postable+active
    accounts.value = accounts.value.filter((a) => a.isActive && a.isPostable);
    accountsLoading.value = false;
  }
}

async function save() {
  error.value = null;

  if (lines.value.length < 2) return (error.value = "Journal must have at least 2 lines.");
  for (const [i, l] of lines.value.entries()) {
    if (!l.accountId) return (error.value = `Line ${i + 1}: select an account.`);
    if (l.debit < 0 || l.credit < 0) return (error.value = `Line ${i + 1}: negative values not allowed.`);
    if ((l.debit === 0 && l.credit === 0) || (l.debit > 0 && l.credit > 0)) return (error.value = `Line ${i + 1}: must have either Debit OR Credit.`);
  }
  if (!balanced.value) return (error.value = `Not balanced. Debit (${totalDebit.value}) != Credit (${totalCredit.value}).`);

  loading.value = true;
  try {
    const payload = {
      entryDate: entryDate.value,
      reference: reference.value || null,
      description: description.value || null,
      lines: lines.value.map((l) => ({
        accountId: l.accountId,
        debit: Number(l.debit || 0),
        credit: Number(l.credit || 0),
        memo: l.memo || null,
      })),
    };

    const res = await apiFetch<any>(`/api/journals`, { method: "POST", body: payload });
    const newId = Number(res?.id ?? res?.Id);
    if (!newId) throw new Error("Created but response did not include id.");
    await router.push(`/journals/${newId}`);
  } catch (e: any) {
    error.value = normalizeErr(e);
  } finally {
    loading.value = false;
  }
}

onMounted(loadAccounts);
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-start justify-between gap-3">
      <div>
        <h1 class="text-2xl font-bold">New Journal</h1>
        <p class="text-sm text-gray-500 dark:text-slate-300">Add lines, ensure it’s balanced, save as Draft.</p>
      </div>

      <NuxtLink to="/journals" class="px-3 py-2 rounded border text-sm border-gray-200 hover:bg-gray-50 dark:border-slate-800 dark:hover:bg-slate-900"> Back </NuxtLink>
    </div>

    <div v-if="error" class="p-3 border border-red-200 bg-red-50 text-red-700 rounded">
      {{ error }}
    </div>

    <div class="border rounded p-4 space-y-4 dark:border-slate-800">
      <div class="grid md:grid-cols-3 gap-3">
        <div>
          <label class="block text-xs text-gray-500 dark:text-slate-300">Entry Date</label>
          <input v-model="entryDate" type="date" class="w-full px-3 py-2 rounded border text-sm border-gray-200 dark:border-slate-800 bg-white dark:bg-slate-950" />
        </div>
        <div>
          <label class="block text-xs text-gray-500 dark:text-slate-300">Reference</label>
          <input v-model="reference" type="text" placeholder="Optional" class="w-full px-3 py-2 rounded border text-sm border-gray-200 dark:border-slate-800 bg-white dark:bg-slate-950" />
        </div>
        <div>
          <label class="block text-xs text-gray-500 dark:text-slate-300">Description</label>
          <input v-model="description" type="text" placeholder="Optional" class="w-full px-3 py-2 rounded border text-sm border-gray-200 dark:border-slate-800 bg-white dark:bg-slate-950" />
        </div>
      </div>

      <div class="border rounded overflow-x-auto dark:border-slate-800">
        <table class="min-w-full text-sm">
          <thead>
            <tr class="bg-gray-50 dark:bg-slate-900">
              <th class="p-2 text-left text-gray-700 dark:text-slate-200">Account</th>
              <th class="p-2 text-right text-gray-700 dark:text-slate-200">Debit</th>
              <th class="p-2 text-right text-gray-700 dark:text-slate-200">Credit</th>
              <th class="p-2 text-left text-gray-700 dark:text-slate-200">Memo</th>
              <th class="p-2 text-right text-gray-700 dark:text-slate-200">—</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(l, i) in lines" :key="i" class="border-t border-gray-200 dark:border-slate-800">
              <td class="p-2 min-w-[320px]">
                <select v-model.number="l.accountId" class="w-full px-2 py-2 rounded border text-sm border-gray-200 dark:border-slate-800 bg-white dark:bg-slate-950" :disabled="accountsLoading">
                  <option :value="null">-- Select account --</option>
                  <option v-for="a in accounts" :key="a.id" :value="a.id">{{ a.code ? `${a.code} - ` : "" }}{{ a.name }}</option>
                </select>
              </td>

              <td class="p-2 text-right">
                <input v-model.number="l.debit" type="number" step="0.01" min="0" class="w-28 px-2 py-2 rounded border text-sm text-right border-gray-200 dark:border-slate-800 bg-white dark:bg-slate-950" @input="setDebit(i)" />
              </td>

              <td class="p-2 text-right">
                <input v-model.number="l.credit" type="number" step="0.01" min="0" class="w-28 px-2 py-2 rounded border text-sm text-right border-gray-200 dark:border-slate-800 bg-white dark:bg-slate-950" @input="setCredit(i)" />
              </td>

              <td class="p-2">
                <input v-model="l.memo" type="text" class="w-full px-2 py-2 rounded border text-sm border-gray-200 dark:border-slate-800 bg-white dark:bg-slate-950" placeholder="Optional" />
              </td>

              <td class="p-2 text-right">
                <button class="px-2 py-1 rounded text-xs border border-gray-200 hover:bg-gray-50 dark:border-slate-800 dark:hover:bg-slate-900" @click="removeLine(i)" :disabled="lines.length <= 2">Remove</button>
              </td>
            </tr>
          </tbody>
          <tfoot>
            <tr class="border-t border-gray-200 dark:border-slate-800 bg-gray-50 dark:bg-slate-900">
              <td class="p-2 font-semibold text-gray-700 dark:text-slate-200">Totals</td>
              <td class="p-2 text-right font-semibold text-gray-700 dark:text-slate-200">{{ totalDebit.toFixed(2) }}</td>
              <td class="p-2 text-right font-semibold text-gray-700 dark:text-slate-200">{{ totalCredit.toFixed(2) }}</td>
              <td class="p-2 text-right" colspan="2">
                <span class="text-xs px-2 py-0.5 rounded" :class="balanced ? 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-200' : 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-200'">
                  {{ balanced ? "Balanced" : "Not balanced" }}
                </span>
              </td>
            </tr>
          </tfoot>
        </table>
      </div>

      <div class="flex items-center justify-between gap-2">
        <button class="px-3 py-2 rounded border text-sm border-gray-200 hover:bg-gray-50 dark:border-slate-800 dark:hover:bg-slate-900" @click="addLine">+ Add Line</button>

        <button class="px-4 py-2 rounded bg-black text-white text-sm hover:bg-gray-900 dark:bg-white dark:text-black dark:hover:bg-gray-100" @click="save" :disabled="loading">
          {{ loading ? "Saving..." : "Create Draft" }}
        </button>
      </div>
    </div>
  </div>
</template>
