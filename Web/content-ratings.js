// ContentRatings plugin - client-side panel injected into the movie detail page.
(function () {
    'use strict';

    var API_BASE = '/ContentRatings';
    var STYLE_ID = 'content-ratings-styles';
    var CONTAINER_CLASS = 'content-ratings-container';

    var DESCRIPTOR_BUCKETS = [
        { keys: ['nudity', 'sex'], label: 'Sex & Nudity', icon: 'visibility_off', color: '#e91e8c' },
        { keys: ['violence', 'gore', 'terror', 'horror'], label: 'Violence & Gore', icon: 'local_fire_department', color: '#e53935' },
        { keys: ['language', 'profanity'], label: 'Language', icon: 'record_voice_over', color: '#fb8c00' },
        { keys: ['drug', 'alcohol', 'smoking', 'substance'], label: 'Substance Use', icon: 'science', color: '#8e24aa' }
    ];

    function bucketFor(descriptor) {
        var lower = descriptor.toLowerCase();
        for (var i = 0; i < DESCRIPTOR_BUCKETS.length; i++) {
            var bucket = DESCRIPTOR_BUCKETS[i];
            for (var j = 0; j < bucket.keys.length; j++) {
                if (lower.indexOf(bucket.keys[j]) !== -1) {
                    return bucket;
                }
            }
        }
        return { label: 'Other', icon: 'info', color: '#607d8b' };
    }

    function injectStyles() {
        if (document.getElementById(STYLE_ID)) return;

        var style = document.createElement('style');
        style.id = STYLE_ID;
        style.textContent = [
            '.' + CONTAINER_CLASS + '{margin:1.5em 0;display:flex;flex-direction:column;gap:1em}',
            '.cr-card{background:rgba(255,255,255,.04);border:1px solid rgba(255,255,255,.08);border-radius:.6em;padding:1.1em 1.3em}',
            '.cr-card h3{display:flex;align-items:center;gap:.4em;margin:0 0 .8em;font-size:1.05em;font-weight:600;letter-spacing:.01em}',
            '.cr-card h3 .md-icon{font-size:1.15em}',
            '.cr-rating-row{display:flex;align-items:center;gap:1em;flex-wrap:wrap;margin-bottom:.9em}',
            '.cr-rating-badge{display:flex;align-items:center;justify-content:center;min-width:2.6em;height:2.6em;padding:0 .5em;border-radius:.4em;font-weight:700;font-size:1.15em;color:#fff}',
            '.cr-rating-meta{display:flex;flex-direction:column;gap:.15em}',
            '.cr-rating-source{font-size:.78em;opacity:.6;text-transform:uppercase;letter-spacing:.06em}',
            '.cr-rating-desc{font-size:.92em;opacity:.85}',
            '.cr-descriptor-tags{display:flex;flex-wrap:wrap;gap:.5em}',
            '.cr-tag{display:flex;align-items:center;gap:.35em;padding:.3em .7em;border-radius:2em;font-size:.85em;font-weight:500;border:1px solid rgba(255,255,255,.12);background:rgba(255,255,255,.05)}',
            '.cr-tag .md-icon{font-size:1em}',
            '.cr-tag-dot{width:.55em;height:.55em;border-radius:50%;flex-shrink:0}',
            '.cr-financial-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(9em,1fr));gap:.9em}',
            '.cr-fin-item{display:flex;flex-direction:column;gap:.2em}',
            '.cr-fin-label{font-size:.75em;opacity:.6;text-transform:uppercase;letter-spacing:.06em}',
            '.cr-fin-value{font-size:1.2em;font-weight:600}',
            '.cr-fin-value.cr-profit{color:#4caf50}',
            '.cr-fin-value.cr-loss{color:#e53935}',
            '.cr-empty{opacity:.6;font-size:.9em}'
        ].join('');
        document.head.appendChild(style);
    }

    function ratingColor(rating) {
        if (!rating) return '#607d8b';
        var r = rating.toUpperCase();
        if (r === 'G' || r === 'U') return '#43a047';
        if (r === 'PG') return '#7cb342';
        if (r === 'PG-13' || r === '12A' || r === '12') return '#fb8c00';
        if (r === 'R' || r === '15' || r === '16') return '#e53935';
        if (r === 'NC-17' || r === '18' || r === 'X') return '#b71c1c';
        return '#607d8b';
    }

    function formatCurrency(value) {
        var num = Number(value);
        if (!num) return null;
        if (num >= 1e9) return '$' + (num / 1e9).toFixed(1) + 'B';
        if (num >= 1e6) return '$' + (num / 1e6).toFixed(1) + 'M';
        if (num >= 1e3) return '$' + (num / 1e3).toFixed(1) + 'K';
        return '$' + num.toLocaleString();
    }

    function escapeHtml(text) {
        var div = document.createElement('div');
        div.textContent = text == null ? '' : String(text);
        return div.innerHTML;
    }

    function buildContentRatingsCard(data) {
        var ratings = data.contentRatings || [];
        var hasAnything = ratings.some(function (r) { return r.rating || (r.descriptors && r.descriptors.length); });
        if (!hasAnything) return '';

        var html = '<div class="cr-card"><h3><span class="md-icon">shield</span>Content Advisory</h3>';

        ratings.forEach(function (rating) {
            if (!rating.rating && (!rating.descriptors || !rating.descriptors.length)) return;
            var color = ratingColor(rating.rating);

            html += '<div class="cr-rating-row">';
            if (rating.rating) {
                html += '<div class="cr-rating-badge" style="background:' + color + '">' + escapeHtml(rating.rating) + '</div>';
            }
            html += '<div class="cr-rating-meta">';
            html += '<span class="cr-rating-source">' + escapeHtml(rating.source || 'Unknown') + (rating.region ? ' · ' + escapeHtml(rating.region) : '') + '</span>';
            if (rating.description) {
                html += '<span class="cr-rating-desc">' + escapeHtml(rating.description) + '</span>';
            }
            html += '</div></div>';

            if (rating.descriptors && rating.descriptors.length) {
                html += '<div class="cr-descriptor-tags">';
                rating.descriptors.forEach(function (descriptor) {
                    var bucket = bucketFor(descriptor);
                    html += '<span class="cr-tag"><span class="cr-tag-dot" style="background:' + bucket.color + '"></span>' + escapeHtml(descriptor) + '</span>';
                });
                html += '</div>';
            }
        });

        html += '</div>';
        return html;
    }

    function buildFinancialCard(data) {
        var fin = data.financialData;
        if (!fin || (!fin.budget && !fin.revenue)) return '';

        var html = '<div class="cr-card"><h3><span class="md-icon">payments</span>Box Office</h3><div class="cr-financial-grid">';

        if (fin.budget > 0) {
            html += '<div class="cr-fin-item"><span class="cr-fin-label">Budget</span><span class="cr-fin-value">' + escapeHtml(fin.budgetFormatted) + '</span></div>';
        }
        if (fin.revenue > 0) {
            html += '<div class="cr-fin-item"><span class="cr-fin-label">Revenue</span><span class="cr-fin-value">' + escapeHtml(fin.revenueFormatted) + '</span></div>';
        }
        if (fin.budget > 0 && fin.revenue > 0) {
            var isProfit = fin.revenue >= fin.budget;
            var sign = isProfit ? '+' : '';
            html += '<div class="cr-fin-item"><span class="cr-fin-label">Profit / Loss</span><span class="cr-fin-value ' + (isProfit ? 'cr-profit' : 'cr-loss') + '">' + sign + escapeHtml(fin.profitLossFormatted) + '</span></div>';
        }

        html += '</div></div>';
        return html;
    }

    function render(data) {
        document.querySelectorAll('.' + CONTAINER_CLASS).forEach(function (el) { el.remove(); });

        var ratingsCard = buildContentRatingsCard(data);
        var financialCard = buildFinancialCard(data);
        if (!ratingsCard && !financialCard) return;

        var anchor = document.querySelector('.detailPagePrimaryContent .detailSectionContent');
        if (!anchor) return;

        injectStyles();

        var container = document.createElement('div');
        container.className = CONTAINER_CLASS;
        container.innerHTML = ratingsCard + financialCard;
        anchor.insertAdjacentElement('afterend', container);
    }

    function getCurrentItemId() {
        var hash = window.location.hash || '';
        var queryIndex = hash.indexOf('?');
        if (queryIndex === -1) return null;

        var params = new URLSearchParams(hash.substring(queryIndex));
        return params.get('id');
    }

    function fetchMovieData(itemId) {
        return fetch(API_BASE + '/Movie/' + itemId, { credentials: 'same-origin' })
            .then(function (response) {
                if (!response.ok) return null;
                return response.json();
            })
            .catch(function () { return null; });
    }

    var lastLoadedItemId = null;

    function tryLoad() {
        var itemId = getCurrentItemId();
        if (!itemId) return;

        var anchor = document.querySelector('.detailPagePrimaryContent .detailSectionContent');
        if (!anchor) return;

        if (itemId === lastLoadedItemId && document.querySelector('.' + CONTAINER_CLASS)) {
            return;
        }

        lastLoadedItemId = itemId;
        fetchMovieData(itemId).then(function (data) {
            if (data && itemId === lastLoadedItemId) {
                render(data);
            }
        });
    }

    var pollHandle = null;

    function onNavigate() {
        document.querySelectorAll('.' + CONTAINER_CLASS).forEach(function (el) { el.remove(); });
        lastLoadedItemId = null;

        if (pollHandle) {
            clearInterval(pollHandle);
            pollHandle = null;
        }

        if (!getCurrentItemId()) return;

        // The detail page template mounts asynchronously; poll briefly until it's ready.
        var attempts = 0;
        pollHandle = setInterval(function () {
            attempts++;
            if (document.querySelector('.detailPagePrimaryContent .detailSectionContent')) {
                clearInterval(pollHandle);
                pollHandle = null;
                tryLoad();
            } else if (attempts > 40) {
                clearInterval(pollHandle);
                pollHandle = null;
            }
        }, 150);
    }

    window.addEventListener('hashchange', onNavigate);
    onNavigate();
})();
