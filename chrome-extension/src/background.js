async function startScanner(tab) {
  if (!tab?.id) {
    return;
  }

  try {
    await chrome.scripting.executeScript({
      target: { tabId: tab.id },
      files: ['content.js'],
    });

    await chrome.tabs.sendMessage(tab.id, { type: 'qr-region:start-selection' });
  } catch (error) {
    console.warn('QR Grab could not start:', error);
  }
}

chrome.action.onClicked.addListener((tab) => {
  startScanner(tab);
});

chrome.commands.onCommand.addListener(async (command) => {
  if (command !== 'scan-visible-area') {
    return;
  }

  const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
  startScanner(tab);
});

chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  if (message?.type === 'qr-region:open-url') {
    try {
      const url = new URL(message.url);

      if (url.protocol !== 'http:' && url.protocol !== 'https:') {
        throw new Error('Only http and https links can be opened directly.');
      }

      chrome.tabs.create({
        url: url.href,
        openerTabId: sender.tab?.id,
      });
      sendResponse({ ok: true });
    } catch (error) {
      sendResponse({ ok: false, error: error?.message || 'Could not open this QR result.' });
    }

    return false;
  }

  if (message?.type !== 'qr-region:capture-visible-tab') {
    return false;
  }

  const windowId = sender.tab?.windowId;

  chrome.tabs.captureVisibleTab(windowId, { format: 'png' }, (dataUrl) => {
    const error = chrome.runtime.lastError;

    if (error || !dataUrl) {
      sendResponse({
        ok: false,
        error: error?.message || 'Could not capture the current tab.',
      });
      return;
    }

    sendResponse({ ok: true, dataUrl });
  });

  return true;
});
