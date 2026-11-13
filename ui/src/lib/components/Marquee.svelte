<!--
  Text, který se nevejde: chvíli stojí na začátku, pomalu dojede na konec, chvíli stojí a vrátí se
  (tam a zpátky, dokola). Okraje se během pohybu jemně ztrácejí. Když se text vejde, nic se neděje.
  Při omezení pohybu se nehýbe a končí třemi tečkami.
-->
<script lang="ts">
  import { onMount } from 'svelte';
  import { motionReduced } from '$lib/motion.svelte';

  interface Props {
    text: string;
    /** Rychlost v px/s. */
    speed?: number;
    /** Pauza na každém konci v ms. */
    pause?: number;
    class?: string;
  }

  let { text, speed = 28, pause = 1600, class: klass = '' }: Props = $props();

  let box: HTMLSpanElement | undefined = $state();
  let inner: HTMLSpanElement | undefined = $state();
  let overflow = $state(0);
  let anim: Animation | null = null;

  function measure() {
    if (!box || !inner) return;
    // scrollWidth kontejneru = celá šířka textu (řádkový prvek sám šířku přetečení nehlásí).
    overflow = Math.max(0, Math.ceil(box.scrollWidth - box.clientWidth));
  }

  $effect(() => {
    void text;
    queueMicrotask(measure);
  });

  // Web Animations: keyframes s pauzou na obou koncích, direction "alternate" = tam a zpátky.
  $effect(() => {
    anim?.cancel();
    anim = null;
    const shift = overflow;
    if (!inner || shift <= 0 || motionReduced()) return;
    const travel = (shift / speed) * 1000;
    const total = travel + pause; // jedna cesta + půl pauzy na každém konci
    const hold = pause / 2 / total;
    anim = inner.animate(
      [
        { transform: 'translateX(0)', offset: 0 },
        { transform: 'translateX(0)', offset: hold, easing: 'ease-in-out' },
        { transform: `translateX(${-shift}px)`, offset: 1 - hold },
        { transform: `translateX(${-shift}px)`, offset: 1 },
      ],
      { duration: total, iterations: Infinity, direction: 'alternate' },
    );
    return () => anim?.cancel();
  });

  onMount(() => {
    const ro = new ResizeObserver(measure);
    if (box) ro.observe(box);
    if (inner) ro.observe(inner);
    return () => ro.disconnect();
  });
</script>

<span class="marquee {klass}" class:moving={overflow > 0 && !motionReduced()} bind:this={box} title={overflow > 0 ? text : undefined}>
  <span class="inner" bind:this={inner}>{text}</span>
</span>

<style>
  .marquee {
    display: block;
    min-width: 0;
    overflow: hidden;
    white-space: nowrap;
    text-overflow: ellipsis;
  }
  .moving {
    /* Jemné ztrácení na okrajích, ať text nekončí ostrou hranou. */
    mask-image: linear-gradient(90deg, transparent 0, #000 6px, #000 calc(100% - 6px), transparent 100%);
    padding: 0 6px;
    margin: 0 -6px;
    text-overflow: clip;
  }
  .moving .inner {
    display: inline-block;
  }
</style>
