import { dishes, regions, sourceNotes } from './data/dishes.js';
import { calculateBMI, calculateDishBMIValue, generateBudgetPlan, getBMICategory, round } from './model.js';

const state = {
  budget: 50,
  heightCm: 170,
  weightKg: 65,
  people: 1,
  days: 3,
  region: 'national',
  plan: null
};

const $ = (selector) => document.querySelector(selector);
const $$ = (selector) => [...document.querySelectorAll(selector)];

function formatMoney(value) {
  return `¥${round(value, 1)}`;
}

function dishCard(dish, profile, meal) {
  const score = calculateDishBMIValue(dish, profile, meal);
  const scoreClass = score >= 78 ? 'excellent' : score >= 62 ? 'good' : 'normal';
  return `
    <article class="dish-card" style="--dish-color: ${dish.color}">
      <div class="dish-plate" aria-hidden="true"><span>${dish.icon}</span></div>
      <div class="dish-body">
        <div class="dish-topline">
          <h4>${dish.name}</h4>
          <span class="score-pill ${scoreClass}">BMI适配 ${score}</span>
        </div>
        <p>${dish.style} · ${dish.cuisine} · ${dish.tags.slice(0, 3).join(' / ')}</p>
        <div class="dish-meta">
          <span>${formatMoney(dish.cost)}/份</span>
          <span>${dish.kcal} kcal</span>
          <span>蛋白 ${dish.protein}g</span>
          <span>纤维 ${dish.fiber}g</span>
        </div>
      </div>
    </article>`;
}

function readInputs() {
  state.heightCm = Number($('#height').value || 170);
  state.weightKg = Number($('#weight').value || 65);
  state.people = Number($('#people').value || 1);
  state.days = Number($("#days").value || 3);
  state.budget = Number($("#budgetInput").value || 50);
  state.region = $("#regionSelect")?.value || "national";
}

function renderBMI() {
  const bmi = calculateBMI(state.heightCm, state.weightKg);
  const category = getBMICategory(bmi);
  $('#bmiValue').textContent = bmi ? bmi.toFixed(1) : '--';
  $('#bmiCategory').textContent = category.label;
  $('#bmiCategory').dataset.tone = category.tone;
  $('#bmiAdvice').textContent = category.advice;
  const pointer = bmi ? Math.min(100, Math.max(0, ((bmi - 15) / 20) * 100)) : 0;
  $('#bmiPointer').style.left = `${pointer}%`;
}

function renderPlan() {
  state.plan = generateBudgetPlan({
    budget: state.budget,
    heightCm: state.heightCm,
    weightKg: state.weightKg,
    people: state.people,
    days: state.days,
    region: state.region
  });

  const plan = state.plan;
  const regionInfo = regions.find((item) => item.id === plan.region) || regions[0];
  $('#heroBudget').textContent = `${plan.days}天 · ${plan.people}人 · ${plan.budget}元/天 · ${regionInfo.name}`;
  $('#grandTotal').textContent = formatMoney(plan.grandTotal);
  $('#compatLine').textContent = '软件平台：Windows 桌面版 / Android Flutter 版';

  $('#summaryCards').innerHTML = `
    <div class="metric-card"><span>用户 BMI</span><strong>${plan.profile.bmi || '--'}</strong><small>${plan.bmiCategory.label}</small></div>
    <div class="metric-card"><span>日预算档</span><strong>${formatMoney(plan.budget)}</strong><small>含早中晚三餐</small></div>
    <div class="metric-card"><span>菜品池</span><strong>${dishes.length}</strong><small>多菜系样式</small></div>
    <div class="metric-card"><span>地区</span><strong>${regionInfo.name}</strong><small>${regionInfo.hint}</small></div>
    <div class="metric-card"><span>预计总价</span><strong>${formatMoney(plan.grandTotal)}</strong><small>${plan.days} 天累计</small></div>
  `;

  $('#dailyPlans').innerHTML = plan.dailyPlans.map((day) => `
    <section class="day-card">
      <header>
        <div>
          <p class="eyebrow">DAY ${day.day}</p>
          <h3>预算 ${formatMoney(day.total.cost)} · BMI适配 ${day.total.bmiValue}</h3>
        </div>
        <div class="day-nutrition">
          <span>${day.total.kcal} kcal</span>
          <span>蛋白 ${day.total.protein}g</span>
          <span>纤维 ${day.total.fiber}g</span>
          <span>三餐BMI ${day.total.bmiValue}</span>
        </div>
      </header>
      <div class="meal-grid three">
        <div class="meal-block">
          <div class="meal-title"><span>🌅 早餐</span><b>${formatMoney(day.breakfastSummary.cost * plan.people)}</b></div>
          ${day.breakfast.map((dish) => dishCard(dish, plan.profile, 'breakfast')).join('')}
        </div>
        <div class="meal-block">
          <div class="meal-title"><span>☀️ 中餐</span><b>${formatMoney(day.lunchSummary.cost * plan.people)}</b></div>
          ${day.lunch.map((dish) => dishCard(dish, plan.profile, 'lunch')).join('')}
        </div>
        <div class="meal-block">
          <div class="meal-title"><span>🌙 晚餐</span><b>${formatMoney(day.dinnerSummary.cost * plan.people)}</b></div>
          ${day.dinner.map((dish) => dishCard(dish, plan.profile, 'dinner')).join('')}
        </div>
      </div>
    </section>
  `).join('');

  $('#shoppingList').innerHTML = plan.shoppingList.map((item) => `
    <li>
      <div><strong>${item.name}</strong><span>用于：${item.dishes.join('、')}</span></div>
      <em>${item.estimatedKg}kg · ¥${item.unitPrice}/kg · ${formatMoney(item.estimatedCost)}</em>
    </li>
  `).join('');
}

function renderGallery() {
  const topDishes = dishes
    .map((dish) => ({ ...dish, value: calculateDishBMIValue(dish, state, 'lunch') }))
    .sort((a, b) => b.value - a.value)
    .slice(0, 12);
  $('#dishGallery').innerHTML = topDishes.map((dish) => `
    <button class="gallery-item" style="--dish-color: ${dish.color}" type="button" title="${dish.name}">
      <span>${dish.icon}</span>
      <strong>${dish.name}</strong>
      <small>${dish.style} · ${dish.value}</small>
    </button>
  `).join('');
}

function renderSources() {
  $('#sourceList').innerHTML = sourceNotes.map((source) => `
    <a href="${source.url}" target="_blank" rel="noreferrer">${source.name}</a>
  `).join('');
}

function render() {
  readInputs();
  renderBMI();
  renderPlan();
  renderGallery();
  renderSources();
}

function bindEvents() {
  ['height', 'weight', 'people', 'days', 'budgetInput', 'regionSelect'].forEach((id) => {
    $(`#${id}`).addEventListener('input', render);
  });
  $('#regen').addEventListener('click', render);
  $('#printList').addEventListener('click', () => window.print());
}

if ('serviceWorker' in navigator) {
  window.addEventListener('load', () => {
    navigator.serviceWorker.register('./sw.js').catch(() => undefined);
  });
}

bindEvents();
render();





