<template>
  <section>
    <h1>{{ title }}</h1>
    <p class="muted">Dieser Bildschirm wird in einem späteren Umsetzungsschritt implementiert.</p>
    <p><router-link to="/">← Back to overview</router-link></p>
  </section>
</template>

<script setup lang="ts">
import { useRoute } from "vue-router";
const route = useRoute();
const title = route.name ? String(route.name).replace(/^\w/, (c) => c.toUpperCase()) : "Section";
</script>