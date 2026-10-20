import { createRouter, createWebHistory } from "vue-router";
import Overview from "../views/Overview.vue";
import Requests from "../views/Requests.vue";
import Approvals from "../views/Approvals.vue";
import Orders from "../views/Orders.vue";
import Placeholder from "../views/Placeholder.vue";

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: "/", name: "overview", component: Overview },
    { path: "/requests", name: "requests", component: Requests },
    { path: "/approvals", name: "approvals", component: Approvals },
    { path: "/orders", name: "orders", component: Orders },
    { path: "/inventory", name: "inventory", component: Placeholder },
  ],
});

export default router;
