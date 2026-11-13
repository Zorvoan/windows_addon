/**
 * Pohyb na jednom místě (jako tokeny v tokens.css, jen pro animace řízené z JS).
 *
 * Pružiny se zadávají jako ve SwiftUI: response (s, přibližně délka jednoho kmitu)
 * a dampingFraction (1 = bez překmitu, méně = lehké doznění).
 *
 * Proč vlastní pružina a ne Spring ze svelte/motion: ta mění rychlost o celé zrychlení
 * v každém snímku bez ohledu na jeho délku, takže na 144Hz monitoru běží znatelně rychleji
 * než na 60Hz. Tady se počítá ve skutečném čase (pevné kroky 1/240 s), na frekvenci nezáleží.
 */
import { prefersReducedMotion } from 'svelte/motion';
import { host } from '$lib/stores/host.svelte';

export interface SpringParams {
  /** Přibližná délka jednoho kmitu v sekundách. */
  response: number;
  /** 1 = bez překmitu, 0,8 = lehké doznění (~1,5 %). */
  dampingFraction: number;
  /** Kdy považovat pružinu za dosednutou (v jednotkách hodnoty, tj. px). */
  precision?: number;
}

export const springs = {
  /** Ostrůvek: měkké roztažení s lehkým doznáním. Stejná křivka je v tokens.css jako --bw-spring. */
  island: { response: 0.42, dampingFraction: 0.8, precision: 0.3 } satisfies SpringParams,
};

/** Doba doběhu pro plynule sledované hodnoty (zvětšení docku), τ v ms. */
export const followTau = { dock: 67 };

/** Má se animace přeskočit? (systémové „Omezit pohyb“ nebo režim výkonu / ruční „Omezit animace“). */
export function motionReduced(): boolean {
  return host.performance.reduced || prefersReducedMotion.current;
}

/**
 * Jeden krok exponenciálního přiblížení k cíli nezávislý na obnovovací frekvenci.
 * dt se omezuje, aby zaseknutí stránky neskočilo rovnou do cíle.
 */
export function follow(current: number, target: number, dtMs: number, tauMs: number): number {
  const dt = Math.min(Math.max(dtMs, 0), 50);
  return current + (target - current) * (1 - Math.exp(-dt / tauMs));
}

const STEP = 1 / 240; // s

/**
 * Pružina pro několik čísel najednou (např. šířka, výška, zaoblení).
 * `value` je reaktivní; `set` vrací Promise: true = dosedla, false = přerušil ji další `set`.
 * Při přerušení se pokračuje ze současné polohy I rychlosti (žádné cuknutí).
 */
export class SpringBox<K extends string> {
  value: Record<K, number> = $state() as Record<K, number>;
  #target: Record<K, number>;
  #velocity: Record<K, number>;
  #params: Required<SpringParams>;
  #frame = 0;
  #last = 0;
  #carry = 0;
  #done: ((settled: boolean) => void) | null = null;

  constructor(initial: Record<K, number>, params: SpringParams) {
    this.value = { ...initial };
    this.#target = { ...initial };
    this.#velocity = Object.fromEntries(Object.keys(initial).map((k) => [k, 0])) as Record<K, number>;
    this.#params = { precision: 0.5, ...params };
  }

  get target(): Record<K, number> {
    return this.#target;
  }

  set(target: Record<K, number>, options?: { instant?: boolean }): Promise<boolean> {
    this.#done?.(false);
    this.#done = null;
    this.#target = { ...target };
    if (options?.instant) {
      this.#stop();
      for (const k in this.#velocity) this.#velocity[k] = 0;
      this.value = { ...target };
      return Promise.resolve(true);
    }
    const promise = new Promise<boolean>((resolve) => (this.#done = resolve));
    if (!this.#frame) {
      this.#last = performance.now();
      this.#carry = 0;
      this.#frame = requestAnimationFrame(this.#tick);
    }
    return promise;
  }

  #tick = (now: number) => {
    const omega = (2 * Math.PI) / this.#params.response;
    const k = omega * omega;
    const c = 2 * this.#params.dampingFraction * omega;
    // Max 1/30 s na snímek: po zaseknutí pokračovat plynule, ne skokem.
    this.#carry += Math.min((now - this.#last) / 1000, 1 / 30);
    this.#last = now;

    const next = { ...this.value };
    while (this.#carry >= STEP) {
      this.#carry -= STEP;
      for (const key in next) {
        // Semi-implicitní Euler: nejdřív rychlost, pak poloha (stabilní pro malé kroky).
        this.#velocity[key] += (k * (this.#target[key] - next[key]) - c * this.#velocity[key]) * STEP;
        next[key] += this.#velocity[key] * STEP;
      }
    }

    const p = this.#params.precision;
    const settled = (Object.keys(next) as K[]).every(
      (key) => Math.abs(this.#target[key] - next[key]) < p && Math.abs(this.#velocity[key]) < p * 10,
    );
    if (settled) {
      for (const key in this.#velocity) this.#velocity[key] = 0;
      this.value = { ...this.#target };
      this.#frame = 0;
      this.#done?.(true);
      this.#done = null;
      return;
    }
    this.value = next;
    this.#frame = requestAnimationFrame(this.#tick);
  };

  #stop() {
    if (this.#frame) cancelAnimationFrame(this.#frame);
    this.#frame = 0;
  }
}
