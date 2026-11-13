/**
 * Pohyb na jednom místě (jako tokeny v tokens.css, jen pro animace řízené z JS).
 * Parametry ostrůvku jsou v apps/island/motion.ts (naměřené z referenční nahrávky).
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
  /** Obecná měkká pružina s lehkým doznáním. Stejná křivka je v tokens.css jako --bw-spring. */
  soft: { response: 0.42, dampingFraction: 0.8, precision: 0.3 } satisfies SpringParams,
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
 * Jedna pružina (jedno číslo, např. šířka). Fyzika ve skutečném čase: step(dt) v sekundách,
 * uvnitř pevné kroky 1/240 s (semi-implicitní Euler), na obnovovací frekvenci nezáleží.
 * Změna cíle za letu zachová polohu i rychlost -> žádné cuknutí.
 */
export class SpringChannel {
  value: number;
  velocity = 0;
  target: number;
  params: Required<SpringParams>;
  #carry = 0;

  constructor(value: number, params: SpringParams) {
    this.value = value;
    this.target = value;
    this.params = { precision: 0.5, ...params };
  }

  retarget(target: number, params?: SpringParams) {
    this.target = target;
    if (params) this.params = { precision: 0.5, ...params };
  }

  /** Počáteční rychlost jako násobek vzdálenosti k cíli za sekundu (z napasování na nahrávku). */
  kick(normalized: number) {
    this.velocity = normalized * (this.target - this.value);
  }

  /** Přidat rychlost (jednotky za sekundu) – „šťouchnutí“, pružina se pak sama vrátí. */
  impulse(velocity: number) {
    this.velocity += velocity;
  }

  /** Okamžitě na hodnotu (bez pohybu). */
  jump(value: number) {
    this.value = this.target = value;
    this.velocity = 0;
    this.#carry = 0;
  }

  step(dt: number) {
    const omega = (2 * Math.PI) / this.params.response;
    const k = omega * omega;
    const c = 2 * this.params.dampingFraction * omega;
    this.#carry += Math.min(Math.max(dt, 0), 1 / 30); // po zaseknutí pokračovat plynule, ne skokem
    while (this.#carry >= STEP) {
      this.#carry -= STEP;
      this.velocity += (k * (this.target - this.value) - c * this.velocity) * STEP;
      this.value += this.velocity * STEP;
    }
    if (this.settled) this.jump(this.target);
  }

  get settled(): boolean {
    const p = this.params.precision;
    return Math.abs(this.target - this.value) < p && Math.abs(this.velocity) < p * 10;
  }
}
