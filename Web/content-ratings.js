// Content Ratings Plugin - Web Client
// This file will be loaded by Jellyfin's web UI

(function() {
    'use strict';

    // Configuration
    const PLUGIN_NAME = 'ContentRatings';
    const API_BASE = '/ContentRatings';

    // Utility functions
    function formatCurrency(value) {
        if (value === null || value === undefined) return 'N/A';
        const num = Number(value);
        if (num >= 1e9) return '$' + (num / 1e9).toFixed(1) + 'B';
        if (num >= 1e6) return '$' + (num / 1e6).toFixed(1) + 'M';
        if (num >= 1e3) return '$' + (num / 1e3).toFixed(1) + 'K';
        return '$' + num.toLocaleString();
    }

    function formatNumber(num) {
        if (num === null || num === undefined) return 'N/A';
        return Number(num).toLocaleString();
    }

    function getRatingColor(rating) {
        if (!rating) return '#666';
        const r = rating.toUpperCase();
        if (r === 'G' || r === 'U' || r === 'TP') return '#4caf50';
        if (r === 'PG' || r === '6' || r === '-10') return '#8bc34a';
        if (r === 'PG-13' || r === '12A' || r === '12' || r === '14A' || r === '-12') return '#ff9800';
        if (r === 'R' || r === '15' || r === '16' || r === '18A' || r === 'MA15+' || r === '-16') return '#f44336';
        if (r === 'NC-17' || r === '18' || r === 'R18' || r === 'R18+' || r === 'X18+' || r === 'A' || r === '-18') return '#b71c1c';
        return '#666';
    }

    function getRatingIcon(rating) {
        if (!rating) return 'help';
        const r = rating.toUpperCase();
        if (r === 'G' || r === 'U' || r === 'TP') return 'child_friendly';
        if (r === 'PG' || r === '6' || r === '-10') return 'family_restroom';
        if (r === 'PG-13' || r === '12A' || r === '12' || r === '14A' || r === '-12') return 'warning';
        if (r === 'R' || r === '15' || r === '16' || r === '18A' || r === 'MA15+' || r === '-16') return 'gpp_maybe';
        if (r === 'NC-17' || r === '18' || r === 'R18' || r === 'R18+' || r === 'X18+' || r === 'A' || r === '-18') => 'block';
        return 'help';
    }

    // Create HTML for content ratings section
    function createContentRatingsHtml(data) {
        if (!data.contentRatings || data.contentRatings.length === 0) {
            return '<div class="content-ratings-empty">No content ratings available</div>';
        }

        let html = '<div class="content-ratings-section">';
        html += '<h3><i class="md-icon">content_cut</i> Content Ratings</h3>';
        html += '<div class="ratings-grid">';

        data.contentRatings.forEach(rating => {
            const color = getRatingColor(rating.rating);
            html += `
                <div class="rating-card" style="border-left-color: ${color}">
                    <div class="rating-header">
                        <span class="rating-source">${rating.source}</span>
                        <span class="rating-region">${rating.region}</span>
                    </div>
                    <div class="rating-main">
                        <span class="rating-value" style="color: ${color}">${rating.rating}</span>
                        <i class="md-icon rating-icon" style="color: ${color}">${getRatingIcon(rating.rating)}</i>
                    </div>
                    ${rating.descriptors && rating.descriptors.length > 0 ? `
                        <div class="rating-descriptors">
                            ${rating.descriptors.map(d => `<span class="descriptor-tag">${d}</span>`).join('')}
                        </div>
                    ` : ''}
                    ${rating.description ? `<div class="rating-description">${rating.description}</div>` : ''}
                </div>
            `;
        });

        html += '</div></div>';
        return html;
    }

    // Create HTML for financial data section
    function createFinancialHtml(data) {
        if (!data.financialData) {
            return '<div class="financial-empty">No financial data available</div>';
        }

        const fin = data.financialData;
        const profitLossClass = fin.profitLoss >= 0 ? 'profit' : 'loss';
        const profitLossPrefix = fin.profitLoss >= 0 ? '+' : '';

        let html = '<div class="financial-section">';
        html += '<h3><i class="md-icon">attach_money</i> Box Office & Budget</h3>';
        html += '<div class="financial-grid">';

        if (fin.budget > 0) {
            html += `
                <div class="financial-card">
                    <div class="financial-label">Budget</div>
                    <div class="financial-value">${fin.budgetFormatted}</div>
                    <div class="financial-source">Source: ${fin.source}</div>
                </div>
            `;
        }

        if (fin.revenue > 0) {
            html += `
                <div class="financial-card">
                    <div class="financial-label">Revenue</div>
                    <div class="financial-value">${fin.revenueFormatted}</div>
                    <div class="financial-source">Source: ${fin.source}</div>
                </div>
            `;
        }

        if (fin.budget > 0 && fin.revenue > 0) {
            html += `
                <div class="financial-card ${profitLossClass}">
                    <div class="financial-label">Profit / Loss</div>
                    <div class="financial-value">${profitLossPrefix}${fin.profitLossFormatted}</div>
                    <div class="financial-roi">ROI: ${profitLossPrefix}${fin.roiPercentage.toFixed(1)}%</div>
                </div>
            `;
        }

        html += '</div></div>';
        return html;
    }

    // Create HTML for age ratings section
    function createAgeRatingsHtml(data) {
        if (!data.ageRatings || data.ageRatings.length === 0) {
            return '<div class="age-ratings-empty">No age ratings available</div>';
        }

        let html = '<div class="age-ratings-section">';
        html += '<h3><i class="md-icon">shield</i> Age Ratings</h3>';
        html += '<div class="ratings-grid">';

        data.ageRatings.forEach(rating => {
            const color = getRatingColor(rating.rating);
            html += `
                <div class="rating-card age-rating-card" style="border-left-color: ${color}">
                    <div class="rating-header">
                        <span class="rating-source">${rating.source}</span>
                        <span class="rating-region">${rating.region}</span>
                    </div>
                    <div class="rating-main">
                        <span class="rating-value" style="color: ${color}">${rating.rating}</span>
                        <i class="md-icon rating-icon" style="color: ${color}">${getRatingIcon(rating.rating)}</i>
                    </div>
                    ${rating.description ? `<div class="rating-description">${rating.description}</div>` : ''}
                </div>
            `;
        });

        html += '</div></div>';
        return html;
    }

    // Main function to load and display enhanced data
    async function loadEnhancedData(itemId) {
        try {
            const response = await fetch(`${API_BASE}/Movie/${itemId}`);
            
            if (!response.ok) {
                if (response.status === 404) {
                    return null;
                }
                throw new Error(`HTTP ${response.status}`);
            }

            const data = await response.json();
            return data;
        } catch (error) {
            console.error('ContentRatings: Error loading data', error);
            return null;
        }
    }

    // Inject the enhanced data into the movie detail page
    function injectEnhancedData(data) {
        // Remove existing content ratings sections
        document.querySelectorAll('.content-ratings-section, .financial-section, .age-ratings-section').forEach(el => el.remove());

        // Find the details section in Jellyfin's detail page
        const detailsSection = document.querySelector('.detailPageContent, .itemDetailPage, [data-detail-page], .detailContent');
        
        if (!detailsSection) {
            console.warn('ContentRatings: Could not find detail page container');
            return;
        }

        // Create container for our enhanced data
        const container = document.createElement('div');
        container.className = 'content-ratings-container';
        container.innerHTML = `
            ${createContentRatingsHtml(data)}
            ${createFinancialHtml(data)}
            ${createAgeRatingsHtml(data)}
        `;

        // Insert after the overview or at the beginning of details
        const overviewSection = detailsSection.querySelector('.overviewSection, .itemOverview, [data-overview]');
        if (overviewSection && overviewSection.parentNode) {
            overviewSection.parentNode.insertBefore(container, overviewSection.nextSibling);
        } else {
            // Insert at the beginning of the details section
            detailsSection.insertBefore(container, detailsSection.firstChild);
        }
    }

    // Initialize when on a movie detail page
    function initialize() {
        // Check if we're on a movie detail page
        const itemId = getItemIdFromUrl();
        
        if (!itemId) {
            return;
        }

        // Wait for page to be ready
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => loadAndInject(itemId));
        } else {
            loadAndInject(itemId);
        }
    }

    function getItemIdFromUrl() {
        // Jellyfin detail page URLs: /web/detail.html?id=<itemId> or /web/index.html#!/detail/<itemId>
        const urlParams = new URLSearchParams(window.location.search);
        let itemId = urlParams.get('id');
        
        if (!itemId) {
            const hash = window.location.hash;
            const match = hash.match(/detail\/([a-f0-9-]+)/i);
            if (match) {
                itemId = match[1];
            }
        }

        return itemId;
    }

    async function loadAndInject(itemId) {
        // Small delay to ensure page is fully rendered
        await new Promise(resolve => setTimeout(resolve, 500));
        
        const data = await loadEnhancedData(itemId);
        
        if (data) {
            injectEnhancedData(data);
        }
    }

    // Listen for navigation changes (Jellyfin is a SPA)
    let lastUrl = location.href;
    new MutationObserver(() => {
        const url = location.href;
        if (url !== lastUrl) {
            lastUrl = url;
            setTimeout(initialize, 100);
        }
    }).observe(document, { subtree: true, childList: true });

    // Also listen for popstate
    window.addEventListener('popstate', () => {
        setTimeout(initialize, 100);
    });

    // Initialize
    initialize();

})();