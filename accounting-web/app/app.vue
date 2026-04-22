<script setup lang="ts">
import AppNavbar from "~/components/AppNavbar.vue";
</script>

<template>
  <div class="min-h-screen bg-white text-gray-900 dark:bg-slate-950 dark:text-slate-100">
    <AppNavbar />

    <main class="mx-auto max-w-7xl px-5 py-6">
      <NuxtPage />
    </main>
  </div>
</template>
