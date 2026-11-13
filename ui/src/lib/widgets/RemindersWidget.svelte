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
    gap: var(--bw-space-2);
    min-height: 0;
  }
  header {
    display: flex;
    justify-content: space-between;
    align-items: baseline;
  }
  .title {
    font-weight: var(--bw-weight-medium);
    font-size: var(--bw-text-sm);
  }
  .count {
    font-size: var(--bw-text-sm);
    color: var(--bw-fg-subtle);
  }
  ul {
    list-style: none;
    margin: 0;
    padding: 0;
    flex: 1;
    overflow-y: auto;
    display: grid;
    align-content: start;
    gap: var(--bw-space-1);
    font-size: var(--bw-text-xs);
  }
  li {
    display: flex;
    align-items: center;
    gap: var(--bw-space-2);
  }
  /* Klikací plocha 20×20 (Apple: minimum na desktopu), viditelný kroužek 12 px uvnitř. */
  .check {
    flex: none;
    display: grid;
    place-items: center;
    width: 20px;
    height: 20px;
    margin: -4px;
    padding: 0;
    border: 0;
    background: none;
    color: var(--bw-accent-fg);
  }
  .check::before {
    content: '';
    grid-area: 1 / 1;
    width: 12px;
    height: 12px;
    border-radius: 50%;
    border: 1.5px solid var(--bw-fg-subtle);
    box-sizing: border-box;
  }
  .check :global(svg) {
    grid-area: 1 / 1;
    position: relative;
  }
  .done .check::before {
    background: var(--bw-accent);
    border-color: var(--bw-accent);
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
    display: grid;
    place-items: center;
    width: 20px;
    height: 20px;
    margin: -4px 0;
    border: 0;
    border-radius: var(--bw-radius-sm);
    background: none;
    color: var(--bw-fg-muted);
    padding: 0;
    opacity: 0;
  }
  li:hover .del,
  .del:focus-visible {
    opacity: 1;
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
