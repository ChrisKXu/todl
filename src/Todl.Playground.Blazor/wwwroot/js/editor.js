export var editor;
export var result;

const hello = [
    'import { Console } from System;',
    '',
    'void Main() {',
    '\tConsole.WriteLine("Hello World!");',
    '}'
].join('\n');

export function init() {
    editor = monaco.editor.create(document.getElementById('editor'), {
        value: hello
    });

    result = monaco.editor.create(document.getElementById('result'), {
        readOnly: true
    });
}

export function getEditorText() {
    return editor.getValue();
}

export function setResultText(text) {
    result.getModel().setValue(text);
}
