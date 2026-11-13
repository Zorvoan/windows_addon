/**
 * Pohyb ostrůvku podle referenční nahrávky (rozbalení / sbalení přehrávače na telefonu), změřené
 * po snímcích (60 fps) a napasované na pružiny:
 *
 *  Rozbalení
 *   1. nafouknutí 0–170 ms: pilulka kolem středu +3 % šířky, +10 % výšky,
 *   2. stažení 170–370 ms: šířka na 87 %, výška zpět; kompaktní obsah se rozmaže a zmizí,
 *   3. roztažení: šířka, výška i svislý střed jednou pružinou (response 0,50 s, tlumení 0,82 →
 *      překmit ~1 %; výška a střed startují s 2× větší počáteční rychlostí); obsah je od začátku v plné velikosti, zmenšený na šířku pilulky,
 *      rozmazaný a zaostřuje se s postupem; pod pilulkou naroste stín.
 *  Sbalení (po prodlevě 33 ms jako v nahrávce; stejná prodleva je i před roztažením)
 *   šířka 0,38 s / 0,60 (podkmit ~8 % = chvíli užší), výška 0,44 s / 0,96 (pomalejší → chvíli
 *   úzká a vysoká), svislý střed 0,49 s / 0,80 (pilulka se stahuje ke středu kompaktní polohy,
 *   takže na konci krátce přesahuje nahoru i dolů – „želé“; střed startuje 2× rychleji než výška,
 *   proto pilulka od začátku stoupá); obsah se hned rozmaže a zmizí,
 *   kompaktní obsah se zaostří, až je pilulka skoro stažená.
 *
 * Chyba napasování křivek na nahrávku: rozbalení 0,002 %, sbalení 0,01–0,03 % (střední kvadratická).
 * Třída nemá nic se Svelte: v každém snímku zavolá onFrame(look) a komponenta to jen vykreslí.
 */
import { SpringChannel, type SpringParams } from '$lib/motion.svelte';

export type IslandMode = 'hidden' | 'compact' | 'expanded' | 'volume';

export interface IslandLook {
  /** Rozměry a poloha pilulky (px v okně ostrůvku). */
  w: number;
  h: number;
  top: number;
  r: number;
  /** 0..1 – síla stínu pod rozbalenou pilulkou. */
  shadow: number;
  compact: { opacity: number; blur: number };
  expanded: { opacity: number; blur: number; scale: number };
  volume: { opacity: number };
}

export const EXPANDED = { w: 400, h: 156, r: 28 };

const P = {
  expand: { response: 0.5, dampingFraction: 0.82, precision: 0.2 },
  collapseW: { response: 0.38, dampingFraction: 0.6, precision: 0.2 },
  collapseH: { response: 0.44, dampingFraction: 0.96, precision: 0.2 },
  collapseY: { response: 0.49, dampingFraction: 0.8, precision: 0.2 },
  other: { response: 0.42, dampingFraction: 0.8, precision: 0.2 },
  /** Pohlcení okna aplikace: měkčí pružina s viditelným zhoupnutím. */
  absorb: { response: 0.45, dampingFraction: 0.6, precision: 0.2 },
} satisfies Record<string, SpringParams>;

/** Šťouchnutí při pohlcení (násobek rozměru za sekundu): ~+12 % šířky, ~+28 % výšky kolem středu. */
const ABSORB_KICK = { w: 2.8, h: 6.5 };

/** Počáteční rychlosti (násobek vzdálenosti k cíli za sekundu), napasované spolu s pružinami. */
const KICK = { expandW: 1, expandH: 2, collapseW: 1, collapseH: 1, collapseY: 2 };

/** Prodleva před rozjezdem pružin (v nahrávce ~2 snímky, než systém zareaguje na puštění prstu). */
const HOLD = 33; // ms

const PRE = { swellEnd: 170, end: 370, swellW: 1.03, swellH: 1.1, squeezeW: 0.87, squeezeH: 1.02 };
const BLUR = { expanded: 6, compact: 4 }; // px (na telefonu ~15 px při 2,8× větší pilulce)

const easeOutSine = (t: number) => Math.sin((t * Math.PI) / 2);
const easeOut15 = (t: number) => 1 - Math.pow(1 - t, 1.5);
const clamp01 = (v: number) => Math.min(1, Math.max(0, v));

/** Časové prolnutí jedné vrstvy (průhlednost; rozmazání se odvozuje). */
class Fade {
  from = 0;
  to = 0;
  start = 0;
  delay = 0;
  dur = 0;
  constructor(value: number) {
    this.from = this.to = value;
  }
  value(now: number): number {
    if (this.dur <= 0) return now >= this.start + this.delay ? this.to : this.from;
    const t = clamp01((now - this.start - this.delay) / this.dur);
    return this.from + (this.to - this.from) * t;
  }
  set(now: number, to: number, dur: number, delay = 0) {
    this.from = this.value(now);
    this.to = to;
    this.start = now;
    this.delay = delay;
    this.dur = dur;
  }
  done(now: number): boolean {
    return now >= this.start + this.delay + this.dur;
  }
}

export class IslandAnimator {
  #w: SpringChannel;
  #h: SpringChannel;
  #cy: SpringChannel;
  #mode: IslandMode = 'hidden';
  #phase: 'pre' | 'spring' = 'spring';
  /** Pružiny se rozjedou až v tento čas (HOLD), pak dostanou počáteční rychlost. */
  #holdUntil = 0;
  #pendingKick: (() => void) | null = null;
  #phaseStart = 0;
  #frame = 0;
  #last = 0;
  /** Rozbalený obsah: výchozí šířka/průhlednost/rozmazání v okamžiku poslední změny směru. */
  #eFrom = { w: 0, opacity: 0, blur: BLUR.expanded };
  #collapsing = false;
  #compactRevealed = false;
  #compact = new Fade(0);
  #volume = new Fade(0);
  #look: IslandLook;

  constructor(
    private top: number,
    private sizeFor: (mode: IslandMode) => { w: number; h: number },
    private onFrame: (look: IslandLook) => void,
    private onSettle: () => void,
  ) {
    const s = sizeFor('hidden');
    this.#w = new SpringChannel(s.w, P.other);
    this.#h = new SpringChannel(s.h, P.other);
    this.#cy = new SpringChannel(top + s.h / 2, P.other);
    this.#look = this.#compose(performance.now());
  }

  get look(): IslandLook {
    return this.#look;
  }

  go(mode: IslandMode, reduced: boolean) {
    const now = performance.now();
    const prev = this.#mode;
    if (mode === prev) return;
    this.#mode = mode;
    const target = this.sizeFor(mode);
    const cyT = this.top + target.h / 2;

    if (reduced) {
      this.#w.jump(target.w);
      this.#h.jump(target.h);
      this.#cy.jump(cyT);
      this.#phase = 'spring';
      this.#pendingKick = null;
      this.#holdUntil = 0;
      this.#collapsing = false;
      this.#compact = new Fade(mode === 'compact' ? 1 : 0);
      this.#volume = new Fade(mode === 'volume' ? 1 : 0);
      this.#eFrom = { w: target.w, opacity: mode === 'expanded' ? 1 : 0, blur: 0 };
      this.#emit(now);
      this.onSettle();
      return;
    }

    const cur = this.#look;
    if (mode === 'expanded') {
      this.#collapsing = false;
      const settledCompact = prev === 'compact' && this.#allSettled() && this.#phase === 'spring';
      // Výchozí stav rozbaleného obsahu = co je vidět teď (při přerušení sbalování nic neblikne).
      this.#eFrom = { w: cur.w, opacity: cur.expanded.opacity, blur: cur.expanded.blur };
      if (settledCompact) {
        this.#phase = 'pre';
        this.#phaseStart = now;
        this.#compact.set(now, 0, 100, PRE.swellEnd);
      } else {
        this.#startExpandSprings();
        this.#compact.set(now, 0, 90);
      }
      this.#volume.set(now, 0, 90);
    } else {
      const fromExpanded = prev === 'expanded';
      if (this.#phase === 'pre') this.#syncChannelsFromLook();
      this.#phase = 'spring';
      this.#collapsing = fromExpanded || this.#collapsing;
      if (this.#collapsing) {
        this.#eFrom = { w: cur.w, opacity: cur.expanded.opacity, blur: cur.expanded.blur };
        this.#w.retarget(target.w, P.collapseW);
        this.#h.retarget(target.h, P.collapseH);
        this.#cy.retarget(cyT, P.collapseY);
        if (fromExpanded) {
          this.#holdUntil = now + HOLD;
          this.#pendingKick = () => {
            this.#w.kick(KICK.collapseW);
            this.#h.kick(KICK.collapseH);
            this.#cy.kick(KICK.collapseY);
          };
        }
        this.#compactRevealed = false;
      } else {
        this.#w.retarget(target.w, P.other);
        this.#h.retarget(target.h, P.other);
        this.#cy.retarget(cyT, P.other);
      }
      if (mode === 'compact' && !this.#collapsing) this.#compact.set(now, 1, 160, 70);
      else if (mode !== 'compact') this.#compact.set(now, 0, 90);
      this.#volume.set(now, mode === 'volume' ? 1 : 0, mode === 'volume' ? 160 : 90, mode === 'volume' ? 70 : 0);
    }
    this.#run();
  }

  /**
   * Okno hrající aplikace vletělo do ostrůvku (jako na telefonu): krátké nafouknutí kolem středu
   * s doznívajícím zhoupnutím. Jen šťouchnutí do pružin – naváže na cokoli, co právě běží.
   */
  absorb(reduced: boolean) {
    if (reduced || this.#phase === 'pre' || this.#mode === 'hidden' || this.#mode === 'expanded') return;
    const c = this.sizeFor(this.#mode);
    this.#w.retarget(c.w, P.absorb);
    this.#h.retarget(c.h, P.absorb);
    this.#w.impulse(c.w * ABSORB_KICK.w);
    this.#h.impulse(c.h * ABSORB_KICK.h);
    this.#run();
  }

  #startExpandSprings(now = performance.now()) {
    this.#phase = 'spring';
    this.#holdUntil = now + HOLD;
    this.#eFrom.w = this.#w.value;
    this.#w.retarget(EXPANDED.w, P.expand);
    this.#h.retarget(EXPANDED.h, P.expand);
    this.#cy.retarget(this.top + EXPANDED.h / 2, P.expand);
    this.#pendingKick = () => {
      this.#w.kick(KICK.expandW);
      this.#h.kick(KICK.expandH);
      this.#cy.kick(KICK.expandH);
    };
  }

  /** Konec / přerušení předfáze: pružiny převezmou aktuální tvar (bez rychlosti – předfáze končí v klidu). */
  #syncChannelsFromLook() {
    const l = this.#look;
    this.#w.jump(l.w);
    this.#h.jump(l.h);
    this.#cy.jump(l.top + l.h / 2);
  }

  #run() {
    if (this.#frame) return;
    this.#last = performance.now();
    this.#frame = requestAnimationFrame(this.#tick);
  }

  #tick = (now: number) => {
    const dt = (now - this.#last) / 1000;
    this.#last = now;
    if (this.#phase === 'pre' && now - this.#phaseStart >= PRE.end) {
      this.#syncChannelsFromLook();
      this.#startExpandSprings(now);
    }
    if (this.#phase === 'spring' && now >= this.#holdUntil) {
      if (this.#pendingKick) {
        this.#pendingKick();
        this.#pendingKick = null;
      }
      this.#w.step(dt);
      this.#h.step(dt);
      this.#cy.step(dt);
    }
    this.#emit(now);
    const busy = this.#phase === 'pre' || this.#pendingKick !== null || !this.#allSettled() || !this.#compact.done(now) || !this.#volume.done(now);
    if (busy) {
      this.#frame = requestAnimationFrame(this.#tick);
    } else {
      this.#frame = 0;
      this.#collapsing = false;
      this.onSettle();
    }
  };

  #allSettled(): boolean {
    return this.#w.settled && this.#h.settled && this.#cy.settled;
  }

  #emit(now: number) {
    this.#look = this.#compose(now);
    this.onFrame(this.#look);
  }

  #compose(now: number): IslandLook {
    let w = this.#w.value;
    let h = this.#h.value;
    let cy = this.#cy.value;

    if (this.#phase === 'pre') {
      // Nafouknutí kolem středu, pak stažení (kotva = střed kompaktní pilulky).
      const c = this.sizeFor('compact');
      const t = now - this.#phaseStart;
      if (t < PRE.swellEnd) {
        const e = easeOutSine(t / PRE.swellEnd);
        w = c.w * (1 + (PRE.swellW - 1) * e);
        h = c.h * (1 + (PRE.swellH - 1) * e);
      } else {
        const e = easeOut15(clamp01((t - PRE.swellEnd) / (PRE.end - PRE.swellEnd)));
        w = c.w * (PRE.swellW + (PRE.squeezeW - PRE.swellW) * e);
        h = c.h * (PRE.swellH + (PRE.squeezeH - PRE.swellH) * e);
      }
      cy = this.top + c.h / 2;
    }

    // Rozbalený obsah: zaostřuje se s postupem roztažení / mizí s postupem stažení.
    let eOpacity = 0;
    let eBlur = BLUR.expanded;
    const from = this.#eFrom;
    if (this.#mode === 'expanded' && this.#phase === 'spring') {
      const p = EXPANDED.w === from.w ? 1 : clamp01((w - from.w) / (EXPANDED.w - from.w));
      eOpacity = from.opacity + (1 - from.opacity) * clamp01(p * 3);
      eBlur = Math.min(from.blur, BLUR.expanded) * (1 - p);
    } else if (this.#mode === 'expanded') {
      eOpacity = from.opacity;
      eBlur = from.blur;
    } else if (this.#collapsing) {
      const tw = this.#w.target;
      const q = from.w === tw ? 1 : clamp01((from.w - w) / (from.w - tw));
      eOpacity = from.opacity * clamp01(1 - q * 1.6);
      eBlur = from.blur + (BLUR.expanded - from.blur) * clamp01(q * 5);
      // Kompaktní obsah naběhne, až je pilulka skoro stažená.
      if (this.#mode === 'compact' && !this.#compactRevealed && q >= 0.95) {
        this.#compactRevealed = true;
        this.#compact.set(now, 1, 170);
      }
    }

    const cOpacity = this.#compact.value(now);
    const r = Math.min(h / 2, EXPANDED.r);
    return {
      w,
      h,
      top: cy - h / 2,
      r,
      shadow: clamp01((h - 28) / (EXPANDED.h - 28)),
      compact: { opacity: cOpacity, blur: BLUR.compact * (1 - cOpacity) },
      expanded: { opacity: eOpacity, blur: Math.max(0, eBlur), scale: Math.max(0.2, w / EXPANDED.w) },
      volume: { opacity: this.#volume.value(now) },
    };
  }
}
