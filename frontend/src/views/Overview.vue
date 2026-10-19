<template>
  <section>
    <h1>Overview</h1>
    <p class="muted">
      StockFlow tracks procurement from request to receipt: request, approval, purchase order, partial receipt, stock.
    </p>
    <div class="card">
      <h2>Phase 1 foundation</h2>
      <p class="muted">
        Backend API, PostgreSQL schema, synthetic seed data and connectivity are wired. Feature screens arrive in later slices.
      </p>
      <ul>
        <li><router-link to="/requests">Requests</router-link> — coming in Phase 2</li>
        <li><router-link to="/orders">Purchase Orders</router-link> — coming in Phase 3</li>
        <li><router-link to="/inventory">Inventory</router-link> — coming in Phase 4</li>
      </ul>
    </div>
  </section>
</template>