<script setup lang="ts">
const cards = [
  { title: "Accounts", desc: "Chart of accounts tree + postable heads.", to: "/accounts" },
  { title: "Journals", desc: "Create, post, delete, reverse journals.", to: "/journals" },
  { title: "Trial Balance", desc: "Totals by account for posted journals.", to: "/reports/trial-balance" },
  { title: "Balance Sheet", desc: "Assets / Liabilities / Equity snapshot.", to: "/reports/balance-sheet" },
  { title: "Profit & Loss", desc: "Income / Expense / Net profit.", to: "/reports/profit-loss" },
  { title: "Periods", desc: "Close months to prevent posting.", to: "/admin/periods" },
];
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-start justify-between gap-3">
      <div>
        <h1 class="text-2xl font-bold">Dashboard</h1>
        <p class="text-sm text-gray-500 dark:text-slate-300">Clean, minimal accounting UI. Keep journals posted and reports will update automatically.</p>
      </div>
      <NuxtLink to="/journals/new" class="px-3 py-2 rounded bg-black text-white text-sm hover:bg-gray-900 dark:bg-white dark:text-black dark:hover:bg-gray-100"> New Journal </NuxtLink>
    </div>

    <div class="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
      <NuxtLink v-for="c in cards" :key="c.to" :to="c.to" class="border rounded p-4 hover:bg-gray-50 transition dark:border-slate-800 dark:hover:bg-slate-900">
        <div class="font-semibold">{{ c.title }}</div>
        <div class="text-sm text-gray-500 dark:text-slate-300 mt-1">
          {{ c.desc }}
        </div>
      </NuxtLink>
    </div>
  </div>
</template>
