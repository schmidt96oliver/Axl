// @ts-check
'use strict';

const vscode = require('vscode');
const { spawn } = require('node:child_process');
const { LanguageClient } = require('vscode-languageclient/node');

// Hard-coded like BaseLibProject.PathName: this extension only ever runs on this machine.
const repoRoot = 'C:\\Users\\schmi\\Source\\Axl';
const serverExe = `${repoRoot}\\artifacts\\lsp\\AxlLsp.exe`;
const deployScript = `${repoRoot}\\deploy-lsp.ps1`;

/** @type {LanguageClient} */
let client;

/** @type {vscode.OutputChannel} */
let deployOutput;

/**
 * Called once, the first time an .axl/.taxl file is opened or the reload command is run.
 * @param {vscode.ExtensionContext} context
 */
async function activate(context) {
    deployOutput = vscode.window.createOutputChannel('Axl Deploy');

    client = new LanguageClient(
        'axl',                  // id: prefix for settings, e.g. "axl.trace.server": "verbose"
        'Axl Language Server',  // name: title of the server's Output channel
        { command: serverExe }, // server: spawned as a child process, talks LSP over stdin/stdout
        { documentSelector: [{ language: 'axl' }, { language: 'taxl' }] },
    );

    context.subscriptions.push(
        deployOutput,
        client,
        vscode.commands.registerCommand('axl.reloadLanguageServer', reloadLanguageServer),
    );

    await client.start();
}

async function reloadLanguageServer() {
    await vscode.window.withProgress(
        { location: vscode.ProgressLocation.Notification, title: 'Axl: Building and deploying language server…' },
        async () => {
            // The running server keeps its DLLs in artifacts/lsp locked, so it has to go before the copy.
            if (client.isRunning()) {
                await client.stop();
            }

            const exitCode = await runDeployScript();

            // Start again either way: if the build failed, the script never copied, so the old server is still intact.
            await client.start();

            if (exitCode !== 0) {
                const choice = await vscode.window.showErrorMessage(
                    `Axl: deploy-lsp.ps1 failed (exit code ${exitCode}). Still running the previous server.`,
                    'Show Output',
                );
                if (choice === 'Show Output') {
                    deployOutput.show();
                }
            }
        },
    );
}

/**
 * Runs deploy-lsp.ps1 and streams its output into the "Axl Deploy" channel.
 * @returns {Promise<number>} the script's exit code
 */
function runDeployScript() {
    deployOutput.clear();
    return new Promise(resolve => {
        const ps = spawn(
            'powershell.exe',
            ['-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', deployScript],
            { cwd: repoRoot, windowsHide: true },
        );
        ps.stdout.on('data', chunk => deployOutput.append(chunk.toString()));
        ps.stderr.on('data', chunk => deployOutput.append(chunk.toString()));
        ps.on('error', err => {
            deployOutput.appendLine(String(err));
            resolve(-1);
        });
        ps.on('close', code => resolve(code ?? -1));
    });
}

async function deactivate() {
    if (client?.isRunning()) {
        await client.stop();
    }
}

module.exports = { activate, deactivate };
