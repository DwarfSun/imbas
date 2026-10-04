// Scroll tracking and link handling for the reading pane (Pages/ReaderPage.razor).

const reportDelayMs = 250;

function progressOf(page) {
    const scrollable = page.scrollHeight - page.clientHeight;
    return scrollable > 0 ? page.scrollTop / scrollable : 0;
}

// Reports the scroll position to .NET once scrolling pauses, and routes clicks on links
// between chapters (marked by ChapterHtml) to .NET instead of navigating the web view.
export function attach(page, dotnet) {
    const onScroll = () => {
        clearTimeout(page.imbasTimer);
        page.imbasTimer = setTimeout(() => dotnet.invokeMethodAsync('OnScrolled', progressOf(page)), reportDelayMs);
    };

    const onClick = event => {
        const link = event.target.closest('a');
        if (!link || !page.contains(link)) {
            return;
        }

        const chapter = link.getAttribute('data-imbas-chapter');
        if (chapter !== null) {
            event.preventDefault();
            dotnet.invokeMethodAsync('OnLinkClicked', parseInt(chapter, 10), link.getAttribute('data-imbas-fragment'));
        } else if (link.getAttribute('href') === '#') {
            event.preventDefault();
        }
    };

    page.addEventListener('scroll', onScroll, { passive: true });
    page.addEventListener('click', onClick);

    return {
        dispose() {
            clearTimeout(page.imbasTimer);
            page.removeEventListener('scroll', onScroll);
            page.removeEventListener('click', onClick);
        },
    };
}

// Scrolls to an anchor if one is given and found, otherwise to a fraction of the chapter.
// Waits for images first, since they change the chapter's height.
export async function scrollTo(page, progress, fragment) {
    clearTimeout(page.imbasTimer);

    await Promise.all(Array.from(page.querySelectorAll('img'))
        .filter(img => !img.complete)
        .map(img => new Promise(resolve => { img.onload = img.onerror = resolve; })));

    if (fragment) {
        const escaped = CSS.escape(fragment);
        const target = page.querySelector(`#${escaped}`) ?? page.querySelector(`[name="${escaped}"]`);
        if (target) {
            target.scrollIntoView();
            return;
        }
    }

    page.scrollTop = (page.scrollHeight - page.clientHeight) * progress;
}
