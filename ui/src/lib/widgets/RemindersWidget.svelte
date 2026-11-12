<!-- Připomínky: přidat (Enter), odškrtnout, smazat. Ukládá C# (store "reminders"). -->
<script lang="ts">
  import Icon from '$lib/components/Icon.svelte';
  import { host } from '$lib/stores/host.svelte';
  import { load, save } from '$lib/stores/kv';

  interface Reminder {
    id: string;
    text: string;
    done: boolean;
  }

  load('reminders');
  let items = $derived(Array.isArray(host.store['reminders']) ? (host.store['reminders'] as Reminder[]) : []);
  let open = $derived(items.filter((r) => !r.done).length);
  let draft = $state('');

  function add() {
    const text = draft.trim();
    if (!text) return;
    save('reminders', [...items, { id: `${Date.now()}`, text, done: false }]);
    draft = '';
  }

  function toggle(id: string) {
    save('reminders', items.map((r) => (r.id === id ? { ...r, done: !r.done } : r)));
  }

  function remove(id: string) {
    save('reminders', items.filter((r) => r.id !== id));
  }
</script>

<div class="reminders">
  <header>
    <span class="title">Připomínky</span>
    <span class="count">{open}</span>
  </header>
  <ul>
    {#each items as r (r.id)}
      <li class:done={r.done}>
        <button type="button" class="check" aria-label={r.done ? 'Označit jako nesplněné' : 'Označit jako splněné'} onclick={() => toggle(r.id)}>
          {#if r.done}<Icon name="check" size={10} strokeWidth={2.4} />{/if}
        </button>
        <span class="text">{r.text}</span>
        <button type="button" class="del" aria-label="Smazat" onclick={() => remove(r.id)}><Icon name="close" size={10} /></button>
      </li>
    {:else}
      <li class="empty">Zatím nic. Napiš první připomínku.</li>
    {/each}
  </ul>
  <input
    type="text"
    placeholder="Přidat připomínku…"
    bind:value={draft}
    onkeydown={(e) => {
      if (e.key === 'Enter') add();
    }}
  />
</div>

<style>
  .reminders {
    height: 100%;
    display: flex;
    flex-direction: column;
    gap: 6px;
    min-height: 0;
  }
  header {
    display: flex;
    justify-content: space-between;
    align-items: baseline;
  }
  .title {
    font-weight: var(--bw-weight-bold);
    font-size: var(--bw-text-sm);
  }
  .count {
    font-size: 20px;
    font-weight: var(--bw-weight-bold);
    color: #f97316;
  }
  ul {
    list-style: none;
    margin: 0;
    padding: 0;
    flex: 1;
    overflow-y: auto;
    display: grid;
    align-content: start;
    gap: 3px;
    font-size: var(--bw-text-xs);
  }
  li {
    display: flex;
    align-items: center;
    gap: 6px;
  }
  .check {
    flex: none;
    display: grid;
    place-items: center;
    width: 13px;
    height: 13px;
    padding: 0;
    border-radius: 50%;
    border: 1.3px solid var(--bw-fg-muted);
    background: none;
    color: white;
  }
  .done .check {
    background: #f97316;
    border-color: #f97316;
  }
  .done .text {
    text-decoration: line-through;
    color: var(--bw-fg-muted);
  }
  .text {
    flex: 1;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
  .del {
    display: none;
    border: 0;
    background: none;
    color: var(--bw-fg-muted);
    padding: 0;
  }
  li:hover .del {
    display: grid;
  }
  .empty {
    color: var(--bw-fg-muted);
  }
  input {
    height: 24px;
    border: 0;
    border-top: 1px solid var(--bw-glass-border);
    background: none;
    color: inherit;
    font: inherit;
    font-size: var(--bw-text-xs);
    padding: 0;
    user-select: text;
    outline: none;
  }
</style>
