const ACTION_TITLE = 'QR Grab';
const START_SELECTION_MESSAGE = 'qr-grab:start-selection';
const CAPTURE_VISIBLE_TAB_MESSAGE = 'qr-grab:capture-visible-tab';
const OPEN_URL_MESSAGE = 'qr-grab:open-url';
const BADGE_RESET_MS = 3500;

async function startScanner(tab) {
  if (!tab?.id) {
    await showActionError(undefined, 'No active tab to scan.');
    return;
  }

  if (!isInjectableTab(tab)) {
    await showActionError(tab.id, 'Chrome blocks QR Grab on this page. Use a normal web page or the Windows app.');
    return;
  }

  try {
    await clearActionStatus(tab.id);

    await chrome.scripting.executeScript({
      target: { tabId: tab.id },
      files: ['content.js'],
    });

    const response = await chrome.tabs.sendMessage(tab.id, { type: START_SELECTION_MESSAGE });

    if (!response?.ok) {
      throw new Error(response?.error || 'The scanner did not start.');
    }
  } catch (error) {
    console.warn('QR Grab could not start:', error);
    await showActionError(tab.id, getFriendlyStartError(error)).catch((badgeError) => {
      console.warn('QR Grab could not show start error:', badgeError);
    });
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
  if (message?.type === OPEN_URL_MESSAGE) {
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

  if (message?.type !== CAPTURE_VISIBLE_TAB_MESSAGE) {
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

function isInjectableTab(tab) {
  if (!tab.url) {
    return false;
  }

  try {
    const { protocol } = new URL(tab.url);
    return protocol === 'http:' || protocol === 'https:' || protocol === 'file:';
  } catch {
    return false;
  }
}

function getFriendlyStartError(error) {
  const message = error?.message || 'Could not start the scanner on this tab.';

  if (/cannot access|chrome:|web store|restricted|receiving end does not exist/i.test(message)) {
    return 'Chrome blocked access to this page. Reload the tab or use the Windows app for system-wide scanning.';
  }

  return message;
}

async function showActionError(tabId, message) {
  const scoped = tabId ? { tabId } : {};

  await chrome.action.setBadgeBackgroundColor({ ...scoped, color: '#dc2626' });
  await chrome.action.setBadgeText({ ...scoped, text: '!' });
  await chrome.action.setTitle({ ...scoped, title: `${ACTION_TITLE}: ${message}` });

  setTimeout(() => {
    clearActionStatus(tabId).catch(() => {});
  }, BADGE_RESET_MS);
}

async function clearActionStatus(tabId) {
  const scoped = tabId ? { tabId } : {};

  await chrome.action.setBadgeText({ ...scoped, text: '' });
  await chrome.action.setTitle({ ...scoped, title: ACTION_TITLE });
}
