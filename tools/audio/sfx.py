"""Sound effect recipes. Each function returns a rendered Track."""

from synth import Track, note_freq, saw, square, triangle


def pulse(duty):
    return lambda p: square(p, duty)


def move():
    """Moving through a menu: a tiny tick."""
    t = Track(0.08)
    t.add_tone(0, 0.025, 1100, pulse(0.5), 0.16, freq_end=900, release=0.02)
    return t


def confirm():
    """Choosing a menu entry: two rising blips."""
    t = Track(0.22)
    t.add_tone(0, 0.05, note_freq("E5"), pulse(0.5), 0.18, release=0.02)
    t.add_tone(0.06, 0.08, note_freq("B5"), pulse(0.5), 0.18, release=0.05)
    return t


def cancel():
    """Backing out of a menu: two falling blips."""
    t = Track(0.22)
    t.add_tone(0, 0.05, note_freq("B4"), pulse(0.5), 0.16, release=0.02)
    t.add_tone(0.06, 0.08, note_freq("E4"), pulse(0.5), 0.16, release=0.05)
    return t


def slash():
    """A weapon hit: a bright swish into a thud."""
    t = Track(0.45)
    t.add_noise(0, 0.12, 0.45, smoothing=0.1, decay_power=1.2, seed=21)
    t.add_tone(0, 0.1, 900, saw, 0.12, freq_end=300, release=0.03)
    t.add_noise(0.08, 0.22, 0.7, smoothing=0.75, decay_power=1.8, seed=22)
    t.add_tone(0.08, 0.16, 150, triangle, 0.7, freq_end=50, release=0.08)
    return t


def critical():
    """A critical hit: the slash with a ringing high accent."""
    t = slash()
    t.add_tone(0.02, 0.25, note_freq("E6"), pulse(0.125), 0.12, release=0.2, vibrato=0.01)
    t.add_tone(0.02, 0.25, note_freq("B6"), pulse(0.125), 0.08, release=0.2)
    return t


def fire():
    """A spell: a rising shimmer that bursts into crackling flames."""
    t = Track(0.8)
    t.add_tone(0, 0.22, 300, pulse(0.125), 0.16, freq_end=1300, release=0.04, vibrato=0.02)
    t.add_noise(0.2, 0.5, 0.6, smoothing=0.5, decay_power=1.4, seed=31)
    t.add_tone(0.2, 0.2, 200, triangle, 0.5, freq_end=60, release=0.1)
    return t


def heal():
    """Healing: a soft rising arpeggio."""
    t = Track(0.9)
    for i, name in enumerate(["C5", "E5", "G5", "C6", "E6"]):
        t.add_tone(i * 0.06, 0.3, note_freq(name), triangle, 0.22, attack=0.01, release=0.25, vibrato=0.006)
    return t


def buff():
    """A buff: a quick bright fanfare."""
    t = Track(0.7)
    for i, name in enumerate(["G4", "C5", "E5", "G5"]):
        t.add_tone(i * 0.07, 0.12 if i < 3 else 0.35, note_freq(name), pulse(0.25), 0.18, release=0.1)
    return t


def curse():
    """Poison or a curse: a wobbling descending buzz."""
    t = Track(0.7)
    t.add_tone(0, 0.5, note_freq("E4"), pulse(0.5), 0.12, freq_end=note_freq("E3"), release=0.1, vibrato=0.04)
    t.add_noise(0.05, 0.4, 0.2, smoothing=0.9, decay_power=1.0, seed=51)
    return t


def stun():
    """A stun: a dizzy two-tone warble."""
    t = Track(0.6)
    for i in range(4):
        t.add_tone(i * 0.11, 0.09, note_freq("A5" if i % 2 == 0 else "E5"), triangle, 0.2, release=0.03)
    return t


def guard():
    """Defending: a metallic clank."""
    t = Track(0.4)
    t.add_tone(0, 0.18, 620, pulse(0.25), 0.14, freq_end=560, release=0.12)
    t.add_tone(0, 0.18, 930, pulse(0.125), 0.08, release=0.12)
    t.add_noise(0, 0.05, 0.3, smoothing=0.2, decay_power=2.0, seed=61)
    return t


def item():
    """Using an item: a pop and a sparkle."""
    t = Track(0.5)
    t.add_tone(0, 0.05, 300, triangle, 0.4, freq_end=700, release=0.03)
    t.add_tone(0.06, 0.2, note_freq("A5"), pulse(0.25), 0.12, release=0.15)
    t.add_tone(0.12, 0.2, note_freq("E6"), pulse(0.25), 0.1, release=0.15)
    return t


def unit_down():
    """A fighter falls: a sinking tone over a low rumble."""
    t = Track(0.9)
    t.add_tone(0, 0.6, note_freq("A4"), pulse(0.25), 0.16, freq_end=note_freq("A2"), release=0.15, vibrato=0.01)
    t.add_noise(0.05, 0.5, 0.5, smoothing=0.9, decay_power=1.5, seed=41)
    t.add_tone(0.05, 0.3, 90, triangle, 0.6, freq_end=40, release=0.1)
    return t


def your_turn():
    """A hero's turn starts: a short horn call."""
    t = Track(0.6)
    for at, name, length in [(0.0, "D5", 0.08), (0.1, "A5", 0.25)]:
        t.add_tone(at, length, note_freq(name), saw, 0.12, attack=0.02, release=0.1, vibrato=0.004)
        t.add_tone(at, length, note_freq(name) / 2, triangle, 0.2, release=0.1)
    return t


def level_up():
    t = Track(1.3)
    for i, name in enumerate(["C5", "E5", "G5", "C6"]):
        t.add_tone(i * 0.09, 0.1, note_freq(name), pulse(0.25), 0.2, release=0.05)
    t.add_tone(0.4, 0.6, note_freq("E6"), pulse(0.25), 0.18, release=0.3, vibrato=0.008)
    t.add_tone(0.4, 0.6, note_freq("C4"), triangle, 0.35, release=0.3)
    return t


def victory():
    t = Track(2.2)
    lead = pulse(0.25)
    for name, at, length in [("G5", 0.0, 0.12), ("G5", 0.14, 0.12), ("G5", 0.28, 0.12),
                             ("C6", 0.42, 0.3), ("A5", 0.76, 0.12), ("B5", 0.9, 0.12), ("C6", 1.04, 0.8)]:
        t.add_tone(at, length, note_freq(name), lead, 0.22, decay=0.06, sustain=0.65, release=0.15,
                   vibrato=0.006 if length > 0.5 else 0.0)
    t.add_tone(0.42, 1.4, note_freq("C3"), triangle, 0.4, release=0.3)
    t.add_tone(1.04, 0.8, note_freq("E5"), pulse(0.125), 0.08, release=0.4)
    return t


def defeat():
    t = Track(2.2)
    for name, at, length in [("G4", 0.0, 0.3), ("Eb4", 0.35, 0.3), ("C4", 0.7, 0.3), ("B3", 1.05, 0.9)]:
        t.add_tone(at, length, note_freq(name), triangle, 0.35, release=0.2, vibrato=0.006)
    t.add_tone(1.05, 0.9, note_freq("G2"), triangle, 0.4, release=0.3)
    return t


def roar():
    """The boss appears: a low growl with a noisy rush."""
    t = Track(1.4)
    t.add_tone(0, 1.0, 70, saw, 0.3, freq_end=45, attack=0.1, release=0.3, vibrato=0.05)
    t.add_tone(0, 1.0, 105, pulse(0.3), 0.15, freq_end=70, attack=0.1, release=0.3, vibrato=0.06)
    t.add_noise(0, 1.1, 0.5, smoothing=0.8, decay_power=0.8, seed=71)
    return t


EFFECTS = {
    "move.ogg": move,
    "confirm.ogg": confirm,
    "cancel.ogg": cancel,
    "slash.ogg": slash,
    "critical.ogg": critical,
    "fire.ogg": fire,
    "heal.ogg": heal,
    "buff.ogg": buff,
    "curse.ogg": curse,
    "stun.ogg": stun,
    "guard.ogg": guard,
    "item.ogg": item,
    "unit_down.ogg": unit_down,
    "your_turn.ogg": your_turn,
    "level_up.ogg": level_up,
    "victory.ogg": victory,
    "defeat.ogg": defeat,
    "roar.ogg": roar,
}
