<template>
  <div class="app-shell">
    <header class="app-header">
      <div class="brand">
        <span class="brand-mark" aria-hidden="true">SF</span>
        <span>StockFlow</span>
        <span class="muted">From Request to Receipt</span>
      </div>
      <nav class="nav" aria-label="Main navigation">
        <router-link to="/">Overview</router-link>
        <router-link to="/requests">Requests</router-link>
        <router-link to="/approvals">Approvals</router-link>
        <router-link to="/orders">Purchase Orders</router-link>
        <router-link to="/inventory">Inventory</router-link>
      </nav>
    </header>
    <main class="app-main">
      <DemoIdentitySwitcher />
      <ConnectivityBadge />
      <router-view />
    </main>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from "vue";
import ConnectivityBadge from "./components/ConnectivityBadge.vue";
import DemoIdentitySwitcher from "./components/DemoIdentitySwitcher.vue";
import { ensureSession } from "./lib/api";

onMounted(() => {
  ensureSession().catch(() => {});
});
</script>
