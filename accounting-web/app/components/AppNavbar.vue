<script setup lang="ts">
import { ref, onMounted } from "vue";

const route = useRoute();

const links = [
  { to: "/", label: "Dashboard" },
  { to: "/accounts", label: "Accounts" },
  { to: "/journals", label: "Journals" },
  { to: "/reports/trial-balance", label: "Trial Balance" },
  { to: "/reports/balance-sheet", label: "Balance Sheet" },
  { to: "/reports/profit-loss", label: "Profit & Loss" },
  { to: "/admin/periods", label: "Periods" },
];

function isActive(path: string) {
  return route.path === path || route.path.startsWith(path + "/");
}

// Theme toggle (class-based)
const isDark = ref(false);

function applyTheme() {
  document.documentElement.classList.toggle("dark", isDark.value);
  localStorage.setItem("theme", isDark.value ? "dark" : "light");
}

function toggleTheme() {
  isDark.value = !isDark.value;
  applyTheme();
}

onMounted(() => {
  const saved = localStorage.getItem("theme");
  if (!saved) {
    const systemDark = window.matchMedia?.("(prefers-color-scheme: dark)")?.matches ?? false;
    isDark.value = systemDark;
  } else {
    isDark.value = saved === "dark";
  }
  applyTheme();
});
</script>

<template>
  <header class="sticky top-0 z-50 border-b bg-white/90 backdrop-blur dark:bg-slate-950/90 dark:border-slate-800">
    <div class="mx-auto max-w-7xl px-5 py-3 flex items-center justify-between gap-3">
      <!-- Brand -->
      <div class="flex items-center gap-3">
        <NuxtLink to="/" class="font-bold tracking-tight text-lg"> Online Double Entry </NuxtLink>
        <span class="text-[11px] text-gray-500 border rounded px-2 py-0.5 dark:text-slate-200 dark:border-slate-700"> Dev </span>
      </div>

      <!-- Desktop nav -->
      <nav class="hidden lg:flex items-center gap-1">
        <NuxtLink v-for="l in links" :key="l.to" :to="l.to" class="px-3 py-2 rounded text-sm border border-transparent hover:bg-gray-50 dark:hover:bg-slate-900" :class="isActive(l.to) ? 'bg-black text-white hover:bg-black dark:bg-white dark:text-black dark:hover:bg-white' : 'text-gray-700 dark:text-slate-200'">
          {{ l.label }}
        </NuxtLink>
      </nav>

      <!-- Right -->
      <div class="flex items-center gap-2">
        <!-- Theme toggle -->
        <button type="button" class="inline-flex items-center gap-2 px-3 py-2 rounded border text-sm border-gray-200 hover:bg-gray-50 dark:border-slate-800 dark:hover:bg-slate-900" @click="toggleTheme">
          <span class="text-xs text-gray-600 dark:text-slate-300">
            {{ isDark ? "Dark" : "Light" }}
          </span>

          <span class="relative inline-flex h-5 w-10 items-center rounded-full transition bg-gray-300 dark:bg-slate-700">
            <span class="inline-block h-4 w-4 transform rounded-full bg-white transition shadow" :class="isDark ? 'translate-x-5' : 'translate-x-1'" />
          </span>
        </button>

        <!-- Mobile menu -->
        <details class="lg:hidden relative">
          <summary class="list-none cursor-pointer px-3 py-2 rounded border text-sm border-gray-200 dark:border-slate-800">Menu</summary>

          <div class="absolute right-0 mt-2 w-64 rounded border bg-white shadow-sm p-2 border-gray-200 dark:bg-slate-950 dark:border-slate-800">
            <NuxtLink v-for="l in links" :key="l.to + '-m'" :to="l.to" class="block px-3 py-2 rounded text-sm hover:bg-gray-50 dark:hover:bg-slate-900" :class="isActive(l.to) ? 'bg-black text-white dark:bg-white dark:text-black' : 'text-gray-700 dark:text-slate-200'">
              {{ l.label }}
            </NuxtLink>
          </div>
        </details>
      </div>
    </div>
  </header>
</template>
