<script setup lang="ts">
type AccountNode = {
  id: number;
  code?: string | null;
  name: string;
  type?: string | number;
  isPostable?: boolean;
  isActive?: boolean;
  children?: AccountNode[];
};

const props = defineProps<{
  nodes: AccountNode[];
  level: number;
  isOpen: (id: number) => boolean;
  toggle: (id: number) => void;
  typeLabel: (t: any) => string;
}>();
</script>

<template>
  <div class="space-y-1">
    <div v-for="n in nodes" :key="n.id">
      <div class="grid grid-cols-12 items-center text-sm rounded hover:bg-gray-50 dark:hover:bg-slate-900" :style="{ paddingLeft: level * 20 + 'px' }">
        <div class="col-span-2 py-2">
          <button class="mr-2 text-xs px-2 py-1 rounded border border-gray-200 dark:border-slate-800" v-if="n.children && n.children.length" @click="toggle(n.id)">
            {{ isOpen(n.id) ? "−" : "+" }}
          </button>

          <span class="font-mono text-xs text-gray-600 dark:text-slate-300">
            {{ n.code || "—" }}
          </span>
        </div>

        <div class="col-span-5 py-2">
          {{ n.name }}
        </div>

        <div class="col-span-2 text-xs text-gray-600 dark:text-slate-300">
          {{ typeLabel(n.type) }}
        </div>

        <div class="col-span-1">
          <span class="text-xs px-2 py-0.5 rounded" :class="n.isPostable ? 'bg-green-100 text-green-800 dark:bg-green-900/30' : 'bg-gray-100 text-gray-700 dark:bg-slate-800'">
            {{ n.isPostable ? "Y" : "N" }}
          </span>
        </div>

        <div class="col-span-2">
          <span class="text-xs px-2 py-0.5 rounded" :class="n.isActive ? 'bg-green-100 text-green-800 dark:bg-green-900/30' : 'bg-red-100 text-red-800 dark:bg-red-900/30'">
            {{ n.isActive ? "Active" : "Inactive" }}
          </span>
        </div>
      </div>

      <!-- Recursive -->
      <div v-if="n.children && n.children.length && isOpen(n.id)" class="ml-4 border-l border-gray-200 dark:border-slate-800 pl-2">
        <AccountTree :nodes="n.children" :level="level + 1" :isOpen="isOpen" :toggle="toggle" :typeLabel="typeLabel" />
      </div>
    </div>
  </div>
</template>
