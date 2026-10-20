<template>
  <section>
    <h1>{{ title }}</h1>
    <p class="muted">{{ t("placeholder.body") }}</p>
    <p><router-link to="/">← Back to overview</router-link></p>
  </section>
</template>

<script setup lang="ts">
import { useRoute } from "vue-router";
import { useI18n } from "../lib/i18n";
const route = useRoute();
const { t } = useI18n();
const title = route.name ? String(route.name).replace(/^\w/, (c) => c.toUpperCase()) : "Section";
</script>