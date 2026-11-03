// Keep the mask grammar aligned with TextMask.cs for prerendering and model binding.
export function parseMask(mask) {
    const tokens = [];
    for (let index = 0; index < mask.length; index++) {
        const character = mask[index];
        if (character === "\\") {
            if (++index === mask.length) throw new Error("A mask cannot end with an escape character.");
            tokens.push({ character: mask[index], isSlot: false });
        } else {
            tokens.push({ character, isSlot: "0A*".includes(character) });
        }
    }
    return tokens;
}

function accepts(slot, value) {
    if (slot === "0") return /^[0-9]$/.test(value);
    if (slot === "A") return /^[a-zA-Z]$/.test(value);
    return /^[a-zA-Z0-9]$/.test(value);
}

export function unmask(value, tokens) {
    let raw = "";
    let position = 0;
    for (let index = 0; index < tokens.length && position < value.length; index++) {
        const token = tokens[index];
        if (!token.isSlot) {
            let literals = "";
            while (index < tokens.length && !tokens[index].isSlot) literals += tokens[index++].character;
            index--;
            if (value.startsWith(literals, position)) position += literals.length;
            continue;
        }
        while (position < value.length && !accepts(token.character, value[position])) position++;
        if (position < value.length) raw += value[position++];
    }
    return raw;
}

export function format(raw, tokens) {
    let value = "";
    let literals = "";
    let position = 0;
    const slotPositions = [];
    for (const token of tokens) {
        if (!token.isSlot) {
            literals += token.character;
            continue;
        }
        while (position < raw.length && !accepts(token.character, raw[position])) position++;
        if (position === raw.length) return { value, slotPositions };
        value += literals;
        literals = "";
        slotPositions.push(value.length);
        value += raw[position++];
    }
    // Closing punctuation appears only after every slot is filled.
    return { value: value + literals, slotPositions };
}

export function attach(input, mask) {
    let tokens = parseMask(mask);

    function handleInput(event) {
        if (event.isComposing) return;
        const caret = input.selectionStart ?? input.value.length;
        const rawBeforeCaret = unmask(input.value.slice(0, caret), tokens).length;
        const result = format(unmask(input.value, tokens), tokens);
        input.value = result.value;
        const nextCaret = rawBeforeCaret === 0
            ? (result.slotPositions[0] ?? 0)
            : (result.slotPositions[Math.min(rawBeforeCaret, result.slotPositions.length) - 1] ?? -1) + 1;
        input.setSelectionRange(nextCaret, nextCaret);
    }

    function handleBeforeInput(event) {
        if (event.isComposing || input.readOnly || input.disabled || input.selectionStart !== input.selectionEnd) return;
        const caret = input.selectionStart;
        const { slotPositions } = format(unmask(input.value, tokens), tokens);
        if (event.inputType === "deleteContentBackward" && caret > 0 && !slotPositions.includes(caret - 1)) {
            const previous = slotPositions.filter(position => position < caret).at(-1);
            if (previous !== undefined) input.setSelectionRange(previous, caret);
        } else if (event.inputType === "deleteContentForward" && !slotPositions.includes(caret)) {
            const next = slotPositions.find(position => position >= caret);
            if (next !== undefined) input.setSelectionRange(caret, next + 1);
        }
    }

    // Format before Blazor's delegated input event reads the displayed value.
    input.addEventListener("input", handleInput);
    input.addEventListener("beforeinput", handleBeforeInput);
    return {
        setMask(nextMask) { tokens = parseMask(nextMask); },
        dispose() {
            input.removeEventListener("input", handleInput);
            input.removeEventListener("beforeinput", handleBeforeInput);
        }
    };
}
