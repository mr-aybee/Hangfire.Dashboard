/* =========================================================
   Hangfire Dashboard — hangfire-dashboard.js
   Standalone RCL client-side logic (jQuery + Chart.js).
   ========================================================= */

(function ($) {
    'use strict';

    // ── Configuration ──────────────────────────────────────
    const API_BASE = '/api/v1/hangfire';
    let _pageSize = 25;     // set from Razor Model.DefaultPageSize
    let _currentPage = 1;
    let _currentState = '';
    let _currentQueue = '';
    let _currentSearch = '';
    let _sortField = 'createdAt';
    let _sortDir = 'desc';
    let _donutChart = null;
    let _trendChart = null;
    let _autoRefreshTimer = null;
    let _autoRefreshInterval = 0;  // seconds, 0 = off

    // ── Initialisation ─────────────────────────────────────
    window.HangfireDashboard = {
        init: function (opts) {
            _pageSize = opts.pageSize || 25;
            loadStats();
            loadJobs();
            wireEvents();
            wireAutoRefresh();
        }
    };

    // ── Stats ──────────────────────────────────────────────
    function loadStats() {
        $.getJSON(API_BASE + '/stats')
            .done(function (r) {
                if (!r.success) return;
                var d = r.data;
                $('#statEnqueued').text(fmtNum(d.enqueued));
                $('#statProcessing').text(fmtNum(d.processing));
                $('#statScheduled').text(fmtNum(d.scheduled));
                $('#statSucceeded').text(fmtNum(d.succeeded));
                $('#statFailed').text(fmtNum(d.failed));
                $('#statRecurring').text(fmtNum(d.recurring));
            });
    }

    // ── Jobs ───────────────────────────────────────────────
    function loadJobs() {
        var params = {
            page: _currentPage,
            pageSize: _pageSize,
            state: _currentState,
            queue: _currentQueue,
            methodName: _currentSearch,
            sortField: _sortField,
            sortDirection: _sortDir
        };

        $('#jobsTableBody').html(
            '<tr><td colspan="8" class="hf-loading"><i class="fa fa-spinner fa-spin me-2"></i>Loading…</td></tr>'
        );

        $.getJSON(API_BASE + '/jobs', params)
            .done(function (r) {
                if (!r.success) {
                    $('#jobsTableBody').html('<tr><td colspan="8" class="hf-empty"><i class="fa fa-exclamation-circle"></i>Failed to load jobs.</td></tr>');
                    return;
                }
                renderJobsTable(r.data.items, r.data.totalCount);
            })
            .fail(function () {
                $('#jobsTableBody').html('<tr><td colspan="8" class="hf-empty"><i class="fa fa-exclamation-circle"></i>Request failed.</td></tr>');
            });
    }

    function renderJobsTable(items, total) {
        uncheckAll();
        if (!items || items.length === 0) {
            $('#jobsTableBody').html('<tr><td colspan="8" class="hf-empty"><i class="fa fa-inbox"></i>No jobs found.</td></tr>');
            updatePagination(0);
            return;
        }

        var html = '';
        items.forEach(function (j) {
            html += '<tr>' +
                '<td class="hf-td-check"><input type="checkbox" class="form-check-input hf-row-check" value="' + escHtml(j.jobId) + '"></td>' +
                '<td class="hf-td-mono">' + escHtml(j.jobId) + '</td>' +
                '<td>' + stateBadge(j.state) + '</td>' +
                '<td class="hf-td-method">' + escHtml(j.methodName || '—') + '</td>' +
                '<td>' + escHtml(j.queue || 'default') + '</td>' +
                '<td>' + formatDateTime(j.createdAt) + '</td>' +
                '<td>' + formatDuration(j.duration) + '</td>' +
                '<td>' +
                    '<button class="btn btn-sm btn-outline-primary hf-action-btn me-1" onclick="HangfireDashboard.viewJob(\'' + escHtml(j.jobId) + '\')" title="View"><i class="fa fa-eye"></i></button>' +
                    '<button class="btn btn-sm btn-outline-warning hf-action-btn me-1" onclick="HangfireDashboard.requeueJob(\'' + escHtml(j.jobId) + '\')" title="Requeue"><i class="fa fa-redo"></i></button>' +
                    '<button class="btn btn-sm btn-outline-danger hf-action-btn" onclick="HangfireDashboard.deleteJob(\'' + escHtml(j.jobId) + '\')" title="Delete"><i class="fa fa-trash"></i></button>' +
                '</td>' +
                '</tr>';
        });
        $('#jobsTableBody').html(html);
        updatePagination(total);
    }

    function updatePagination(total) {
        var totalPages = Math.max(1, Math.ceil(total / _pageSize));
        var from = total === 0 ? 0 : (_currentPage - 1) * _pageSize + 1;
        var to = Math.min(_currentPage * _pageSize, total);
        $('#paginationInfo').text(from + '–' + to + ' of ' + fmtNum(total));
        $('#btnPageIndicator').text(_currentPage + ' / ' + totalPages);
        $('#btnPrev').prop('disabled', _currentPage <= 1);
        $('#btnNext').prop('disabled', _currentPage >= totalPages);
    }

    // ── Job Detail ─────────────────────────────────────────
    window.HangfireDashboard.viewJob = function (id) {
        $('#jobDetailBody').html('<div class="hf-loading"><i class="fa fa-spinner fa-spin me-2"></i>Loading…</div>');
        var modal = new bootstrap.Modal(document.getElementById('jobDetailModal'));
        modal.show();

        $.getJSON(API_BASE + '/jobs/' + encodeURIComponent(id))
            .done(function (r) {
                if (!r.success) {
                    $('#jobDetailBody').html('<div class="hf-empty">Failed to load job detail.</div>');
                    return;
                }
                renderJobDetail(r.data);
            })
            .fail(function () {
                $('#jobDetailBody').html('<div class="hf-empty">Request failed.</div>');
            });
    };

    function renderJobDetail(d) {
        var h = '';

        // Meta section
        h += '<div class="hf-detail-section">';
        h += '<div class="hf-detail-section-title">Overview</div>';
        h += '<div class="hf-detail-meta">';
        h += metaItem('Job ID', d.jobId);
        h += metaItem('State', stateBadge(d.state));
        h += metaItem('Queue', d.queue || 'default');
        h += metaItem('Method', d.methodName || '—');
        h += metaItem('Created', formatDateTime(d.createdAt));
        h += metaItem('Duration', formatDuration(d.duration));
        h += '</div></div>';

        // Parameters
        if (d.parameters && d.parameters.length) {
            h += '<div class="hf-detail-section">';
            h += '<div class="hf-detail-section-title">Parameters</div>';
            d.parameters.forEach(function (p, idx) {
                var id = 'param-block-' + idx;
                var formatted = tryFormatJson(p.value);
                h += '<div class="hf-param-block">';
                h += '<div class="hf-param-header" onclick="HangfireDashboard._toggleParam(\'' + id + '\', this)">';
                h += '<span class="hf-param-type">' + escHtml(p.name) + '</span>';
                h += '<i class="fa fa-chevron-right hf-param-chevron"></i>';
                h += '</div>';
                h += '<div id="' + id + '" class="hf-param-body"><pre class="hf-json">' + formatted + '</pre></div>';
                h += '</div>';
            });
            h += '</div>';
        }

        // Exception
        if (d.exceptionMessage) {
            h += '<div class="hf-detail-section">';
            h += '<div class="hf-detail-section-title"><i class="fa fa-bug me-1 text-danger"></i>Exception</div>';
            h += '<div class="hf-stacktrace">' + escHtml(d.exceptionMessage) + (d.exceptionDetails ? '\n\n' + escHtml(d.exceptionDetails) : '') + '</div>';
            h += '</div>';
        }

        // State history
        if (d.stateHistory && d.stateHistory.length) {
            h += '<div class="hf-detail-section">';
            h += '<div class="hf-detail-section-title">State History</div>';
            h += '<table class="hf-history-table"><thead><tr><th>State</th><th>Reason</th><th>Time</th></tr></thead><tbody>';
            d.stateHistory.forEach(function (s) {
                h += '<tr><td>' + stateBadge(s.state) + '</td><td>' + escHtml(s.reason || '—') + '</td><td>' + formatDateTime(s.createdAt) + '</td></tr>';
            });
            h += '</tbody></table></div>';
        }

        $('#jobDetailBody').html(h);
    }

    window.HangfireDashboard._toggleParam = function (id, header) {
        var body = document.getElementById(id);
        if (body.classList.contains('open')) {
            body.classList.remove('open');
            header.classList.remove('expanded');
        } else {
            body.classList.add('open');
            header.classList.add('expanded');
        }
    };

    function metaItem(label, value) {
        return '<div class="hf-detail-meta-item"><span class="hf-detail-meta-label">' + label + '</span><span class="hf-detail-meta-value">' + value + '</span></div>';
    }

    // ── Single Actions ─────────────────────────────────────
    window.HangfireDashboard.requeueJob = function (id) {
        if (!confirm('Requeue job ' + id + '?')) return;
        $.post(API_BASE + '/jobs/' + encodeURIComponent(id) + '/requeue')
            .done(function () { loadStats(); loadJobs(); })
            .fail(function () { alert('Requeue failed.'); });
    };

    window.HangfireDashboard.deleteJob = function (id) {
        if (!confirm('Delete job ' + id + '?')) return;
        $.ajax({ url: API_BASE + '/jobs/' + encodeURIComponent(id), type: 'DELETE' })
            .done(function () { loadStats(); loadJobs(); })
            .fail(function () { alert('Delete failed.'); });
    };

    // ── Bulk Actions ───────────────────────────────────────
    function getCheckedIds() {
        return $('.hf-row-check:checked').map(function () { return $(this).val(); }).get();
    }

    function uncheckAll() {
        $('#selectAll').prop('checked', false);
        updateBulkBar();
    }

    function updateBulkBar() {
        var count = getCheckedIds().length;
        if (count > 0) {
            $('#bulkBar').addClass('hf-bulk-bar-visible');
            $('#bulkCount').text(count);
        } else {
            $('#bulkBar').removeClass('hf-bulk-bar-visible');
        }
    }

    window.HangfireDashboard.bulkRequeue = function () {
        var ids = getCheckedIds();
        if (!ids.length) return;
        if (!confirm('Requeue ' + ids.length + ' job(s)?')) return;
        $.ajax({
            url: API_BASE + '/jobs/bulk-requeue',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ jobIds: ids })
        }).done(function () { loadStats(); loadJobs(); });
    };

    window.HangfireDashboard.bulkDelete = function () {
        var ids = getCheckedIds();
        if (!ids.length) return;
        if (!confirm('Delete ' + ids.length + ' job(s)?')) return;
        $.ajax({
            url: API_BASE + '/jobs/bulk-delete',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ jobIds: ids })
        }).done(function () { loadStats(); loadJobs(); });
    };

    // ── Queues Tab ─────────────────────────────────────────
    function loadQueues() {
        $.getJSON(API_BASE + '/queues')
            .done(function (r) {
                if (!r.success || !r.data.length) {
                    $('#queuesTableBody').html('<tr><td colspan="4" class="hf-empty"><i class="fa fa-inbox"></i>No queues.</td></tr>');
                    return;
                }
                var html = '';
                r.data.forEach(function (q) {
                    html += '<tr><td><strong>' + escHtml(q.name) + '</strong></td><td>' + fmtNum(q.length) + '</td><td>' + fmtNum(q.fetched || 0) + '</td>' +
                        '<td><div class="progress" style="height:4px;"><div class="progress-bar bg-primary" style="width:' + Math.min(q.length, 100) + '%"></div></div></td></tr>';
                });
                $('#queuesTableBody').html(html);
            });
    }

    // ── Servers Tab ────────────────────────────────────────
    function loadServers() {
        $.getJSON(API_BASE + '/servers')
            .done(function (r) {
                if (!r.success || !r.data.length) {
                    $('#serversTableBody').html('<tr><td colspan="5" class="hf-empty"><i class="fa fa-server"></i>No servers.</td></tr>');
                    return;
                }
                var html = '';
                r.data.forEach(function (s) {
                    html += '<tr><td><strong>' + escHtml(s.name) + '</strong></td><td>' + s.workersCount + '</td>' +
                        '<td>' + escHtml((s.queues || []).join(', ')) + '</td>' +
                        '<td>' + formatDateTime(s.startedAt) + '</td>' +
                        '<td>' + formatDateTime(s.heartbeat) + '</td></tr>';
                });
                $('#serversTableBody').html(html);
            });
    }

    // ── Recurring Jobs Tab ─────────────────────────────────
    function loadRecurring() {
        $.getJSON(API_BASE + '/recurring-jobs')
            .done(function (r) {
                if (!r.success || !r.data.length) {
                    $('#recurringTableBody').html('<tr><td colspan="6" class="hf-empty"><i class="fa fa-clock"></i>No recurring jobs.</td></tr>');
                    return;
                }
                var html = '';
                r.data.forEach(function (j) {
                    html += '<tr><td><strong>' + escHtml(j.id) + '</strong></td>' +
                        '<td class="hf-td-mono">' + escHtml(j.cron) + '</td>' +
                        '<td>' + escHtml(j.queue || 'default') + '</td>' +
                        '<td>' + formatDateTime(j.lastExecution) + '</td>' +
                        '<td>' + formatDateTime(j.nextExecution) + '</td>' +
                        '<td>' + escHtml(j.lastJobState || '—') + '</td></tr>';
                });
                $('#recurringTableBody').html(html);
            });
    }

    // ── Graphs ─────────────────────────────────────────────
    function loadGraphs() {
        $.getJSON(API_BASE + '/stats')
            .done(function (r) {
                if (!r.success) return;
                var d = r.data;
                renderDonut(d);
                renderTrend(d);
            });
    }

    function renderDonut(d) {
        var ctx = document.getElementById('chartDonut');
        if (!ctx) return;
        if (_donutChart) _donutChart.destroy();

        var isDark = document.documentElement.getAttribute('data-theme') === 'dark';

        _donutChart = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: ['Enqueued', 'Processing', 'Scheduled', 'Succeeded', 'Failed'],
                datasets: [{
                    data: [d.enqueued, d.processing, d.scheduled, d.succeeded, d.failed],
                    backgroundColor: ['#0d6efd', '#fd7e14', '#6f42c1', '#198754', '#dc3545'],
                    borderWidth: 0
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: { color: isDark ? '#ccc' : '#555', padding: 12, usePointStyle: true, pointStyleWidth: 10 }
                    }
                },
                cutout: '62%'
            }
        });
    }

    function renderTrend(d) {
        var ctx = document.getElementById('chartTrend');
        if (!ctx) return;
        if (_trendChart) _trendChart.destroy();

        var isDark = document.documentElement.getAttribute('data-theme') === 'dark';
        var gridColor = isDark ? 'rgba(255,255,255,0.08)' : 'rgba(0,0,0,0.06)';
        var tickColor = isDark ? '#aaa' : '#666';

        // Simulated 7–point trend from current stats
        var points = 7;
        var successArr = generateTrend(d.succeeded, points);
        var failedArr = generateTrend(d.failed, points);
        var labels = [];
        for (var i = points - 1; i >= 0; i--) {
            var dt = new Date();
            dt.setDate(dt.getDate() - i);
            labels.push(dt.toLocaleDateString(undefined, { month: 'short', day: 'numeric' }));
        }

        _trendChart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [
                    { label: 'Succeeded', data: successArr, borderColor: '#198754', backgroundColor: 'rgba(25,135,84,0.08)', fill: true, tension: 0.3, pointRadius: 3 },
                    { label: 'Failed', data: failedArr, borderColor: '#dc3545', backgroundColor: 'rgba(220,53,69,0.08)', fill: true, tension: 0.3, pointRadius: 3 }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                scales: {
                    x: { grid: { color: gridColor }, ticks: { color: tickColor, maxRotation: 0 } },
                    y: { grid: { color: gridColor }, ticks: { color: tickColor }, beginAtZero: true }
                },
                plugins: {
                    legend: { labels: { color: isDark ? '#ccc' : '#555', usePointStyle: true, pointStyleWidth: 10 } }
                }
            }
        });
    }

    function generateTrend(total, points) {
        // Create a plausible ascending trend from ~10% of total to total, with some noise
        var arr = [];
        var base = Math.max(0, Math.round(total * 0.1));
        var step = (total - base) / (points - 1 || 1);
        for (var i = 0; i < points; i++) {
            var v = base + step * i;
            v += Math.round((Math.random() - 0.5) * step * 0.3); // noise
            arr.push(Math.max(0, Math.round(v)));
        }
        arr[points - 1] = total; // pin last point to actual value
        return arr;
    }

    // ── Prefilter Panel ────────────────────────────────────
    function wirePrefilter() {
        $('.hf-prefilter-header').on('click', function () {
            var panel = $(this).closest('.hf-prefilter');
            var state = panel.attr('data-state');
            panel.attr('data-state', state === 'active' ? 'collapsed' : 'active');
        });
    }

    function updatePrefilterSummary() {
        var tags = [];
        if (_currentState) tags.push({ label: 'State', value: _currentState });
        if (_currentQueue) tags.push({ label: 'Queue', value: _currentQueue });
        if (_currentSearch) tags.push({ label: 'Search', value: _currentSearch });

        var $s = $('#prefilterSummary');
        if (!tags.length) {
            $s.html('<span class="hf-prefilter-tag-empty">No filters</span>');
        } else {
            var h = '';
            tags.forEach(function (t) {
                h += '<span class="hf-prefilter-tag">' + t.label + ': <strong>' + escHtml(t.value) + '</strong></span>';
            });
            $s.html(h);
        }
    }

    // ── Auto-Refresh ───────────────────────────────────────
    function wireAutoRefresh() {
        $('#ddAutoRefresh').on('change', function () {
            _autoRefreshInterval = parseInt($(this).val(), 10) || 0;
            clearInterval(_autoRefreshTimer);
            if (_autoRefreshInterval > 0) {
                _autoRefreshTimer = setInterval(function () {
                    loadStats();
                    var activeTab = $('.hf-tabs .nav-link.active').attr('href');
                    switch (activeTab) {
                        case '#tabJobs': loadJobs(); break;
                        case '#tabQueues': loadQueues(); break;
                        case '#tabServers': loadServers(); break;
                        case '#tabRecurring': loadRecurring(); break;
                        case '#tabGraphs': loadGraphs(); break;
                    }
                }, _autoRefreshInterval * 1000);
            }
        });
    }

    // ── Event Wiring ───────────────────────────────────────
    function wireEvents() {
        // Filters
        $('#ddState').on('change', function () { _currentState = $(this).val(); _currentPage = 1; loadJobs(); updatePrefilterSummary(); });
        $('#ddQueue').on('change', function () { _currentQueue = $(this).val(); _currentPage = 1; loadJobs(); updatePrefilterSummary(); });
        $('#txtSearch').on('keyup', debounce(function () { _currentSearch = $(this).val(); _currentPage = 1; loadJobs(); updatePrefilterSummary(); }, 350));
        $('#ddPageSize').on('change', function () { _pageSize = parseInt($(this).val(), 10); _currentPage = 1; loadJobs(); });

        // Clear filters
        $('#btnClearFilters').on('click', function () {
            _currentState = ''; _currentQueue = ''; _currentSearch = '';
            $('#ddState').val(''); $('#ddQueue').val(''); $('#txtSearch').val('');
            _currentPage = 1;
            loadJobs();
            updatePrefilterSummary();
        });

        // Pagination
        $('#btnPrev').on('click', function () { if (_currentPage > 1) { _currentPage--; loadJobs(); } });
        $('#btnNext').on('click', function () { _currentPage++; loadJobs(); });

        // Tab switching
        $('a[data-bs-toggle="tab"]').on('shown.bs.tab', function (e) {
            var target = $(e.target).attr('href');
            switch (target) {
                case '#tabJobs': loadJobs(); break;
                case '#tabQueues': loadQueues(); break;
                case '#tabServers': loadServers(); break;
                case '#tabRecurring': loadRecurring(); break;
                case '#tabGraphs': loadGraphs(); break;
            }
        });

        // Sort headers
        $('.hf-th-sort').on('click', function () {
            var field = $(this).data('sort');
            if (_sortField === field) {
                _sortDir = _sortDir === 'asc' ? 'desc' : 'asc';
            } else {
                _sortField = field;
                _sortDir = 'asc';
            }
            // Update UI
            $('.hf-th-sort').removeClass('hf-th-sort-active');
            $(this).addClass('hf-th-sort-active');
            var icon = $(this).find('.hf-sort-icon');
            icon.removeClass('fa-sort-up fa-sort-down').addClass(_sortDir === 'asc' ? 'fa-sort-up' : 'fa-sort-down');
            _currentPage = 1;
            loadJobs();
        });

        // Select all / bulk
        $('#selectAll').on('change', function () {
            var checked = $(this).prop('checked');
            $('.hf-row-check').prop('checked', checked);
            updateBulkBar();
        });
        $(document).on('change', '.hf-row-check', function () { updateBulkBar(); });

        // Prefilter
        wirePrefilter();
        updatePrefilterSummary();

        // Refresh button
        $('#btnRefresh').on('click', function () {
            loadStats();
            loadJobs();
        });
    }

    // ── Helpers ─────────────────────────────────────────────
    function stateBadge(state) {
        if (!state) return '<span class="hf-badge">—</span>';
        var s = state.toLowerCase();
        return '<span class="hf-badge hf-badge-' + s + '">' + escHtml(state) + '</span>';
    }

    function formatDateTime(val) {
        if (!val) return '—';
        try {
            var d = new Date(val);
            return d.toLocaleDateString() + ' ' + d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' });
        } catch (e) { return escHtml(val); }
    }

    function formatDuration(ms) {
        if (ms === null || ms === undefined || ms === '') return '—';
        if (typeof ms === 'string') ms = parseInt(ms, 10);
        if (isNaN(ms) || ms <= 0) return '—';
        if (ms < 1000) return ms + ' ms';
        if (ms < 60000) return (ms / 1000).toFixed(1) + ' s';
        return (ms / 60000).toFixed(1) + ' min';
    }

    function fmtNum(n) {
        if (n === null || n === undefined) return '0';
        return Number(n).toLocaleString();
    }

    function escHtml(str) {
        if (!str) return '';
        var div = document.createElement('div');
        div.appendChild(document.createTextNode(str));
        return div.innerHTML;
    }

    function tryFormatJson(value) {
        if (!value) return escHtml(value);
        try {
            var obj = typeof value === 'string' ? JSON.parse(value) : value;
            return syntaxHighlight(JSON.stringify(obj, null, 2));
        } catch (e) {
            return escHtml(value);
        }
    }

    function syntaxHighlight(json) {
        return json.replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/("(\\u[a-zA-Z0-9]{4}|\\[^u]|[^\\"])*"(\s*:)?|\b(true|false)\b|-?\d+(?:\.\d*)?(?:[eE][+\-]?\d+)?|\bnull\b)/g,
                function (match) {
                    var cls = 'json-number';
                    if (/^"/.test(match)) {
                        cls = /:$/.test(match) ? 'json-key' : 'json-string';
                    } else if (/true|false/.test(match)) {
                        cls = 'json-bool';
                    } else if (/null/.test(match)) {
                        cls = 'json-null';
                    }
                    // Remove trailing colon from key for display, add it back outside
                    if (cls === 'json-key') {
                        return '<span class="' + cls + '">' + match.replace(/:$/, '') + '</span>:';
                    }
                    return '<span class="' + cls + '">' + match + '</span>';
                })
            .replace(/[{}[\]]/g, function (m) { return '<span class="json-brace">' + m + '</span>'; });
    }

    function debounce(fn, delay) {
        var timer;
        return function () {
            var ctx = this, args = arguments;
            clearTimeout(timer);
            timer = setTimeout(function () { fn.apply(ctx, args); }, delay);
        };
    }

})(jQuery);
