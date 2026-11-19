(function (window, $) {
    if (window.SmcRightsTree) return;

    window.SmcRightsTree = {
        bind: function ($el, options) {
            options = options || {};
            const checkClass = options.checkClass || "smc-check";
            const cellClass = options.cellClass || "smc-check-cell";
            const nameClass = options.nameClass || "smc-check-name";
            const caption = options.caption || "List of functions";
            let childrenByParent = {};
            let checkedSet = {};
            let tree = null;
            let ready = false;
            let boundKey = null;
            let boundSig = "";

            function indexTree(data) {
                childrenByParent = {};
                (data || []).forEach(function (row) {
                    const p = row.parentId == null ? 0 : row.parentId;
                    if (!childrenByParent[p]) childrenByParent[p] = [];
                    childrenByParent[p].push(row.id);
                });
            }

            function expandedKeysFrom(data) {
                const keys = [];
                (data || []).forEach(function (row) {
                    const p = row.parentId == null ? 0 : row.parentId;
                    if (p === 0 && row.id < 0) keys.push(row.id);
                });
                return keys;
            }

            function leafIdsOfKey(key) {
                const ids = [];
                function walk(id) {
                    const kids = childrenByParent[id] || [];
                    if (!kids.length) {
                        if (id > 0) ids.push(id);
                        return;
                    }
                    kids.forEach(walk);
                }
                walk(key);
                return ids;
            }

            function checkStateByKey(key) {
                const leaves = leafIdsOfKey(key);
                let count = 0;
                leaves.forEach(function (id) { if (checkedSet[id]) count++; });
                return { count: count, total: leaves.length };
            }

            function fillCheckbox(box, key) {
                if (!box) return;
                const st = checkStateByKey(key);
                box.checked = st.total > 0 && st.count === st.total;
                box.indeterminate = st.count > 0 && st.count < st.total;
            }

            function syncCheckboxes() {
                $el.find("input." + checkClass).each(function () {
                    const key = parseInt(this.getAttribute("data-key"), 10);
                    fillCheckbox(this, key);
                });
            }

            function toggleKey(key) {
                key = parseInt(key, 10);
                if (isNaN(key)) return;
                const ids = leafIdsOfKey(key);
                if (!ids.length) return;
                const st = checkStateByKey(key);
                const on = st.count === 0;
                ids.forEach(function (id) {
                    if (on) checkedSet[id] = true;
                    else delete checkedSet[id];
                });
                setTimeout(syncCheckboxes, 0);
            }

            function bindCheckClicks() {
                const host = $el[0];
                if (!host || host._smcCheckBound) return;
                host._smcCheckBound = true;
                host.addEventListener("click", function (ev) {
                    const t = ev.target;
                    if (!t || !t.closest) return;
                    const box = t.classList && t.classList.contains(checkClass)
                        ? t
                        : (t.closest("." + cellClass) && t.closest("." + cellClass).querySelector("input." + checkClass));
                    if (!box) return;
                    ev.preventDefault();
                    ev.stopPropagation();
                    toggleKey(box.getAttribute("data-key"));
                }, true);
            }

            function applyGranted(ids) {
                checkedSet = {};
                (ids || []).forEach(function (id) {
                    id = parseInt(id, 10);
                    if (id > 0) checkedSet[id] = true;
                });
                ready = true;
                syncCheckboxes();
            }

            function collect() {
                if (!ready) return null;
                return Object.keys(checkedSet)
                    .map(function (k) { return parseInt(k, 10); })
                    .filter(function (k) { return k > 0 && checkedSet[k]; })
                    .join(",");
            }

            function init(data) {
                data = data || [];
                ready = false;
                bindCheckClicks();
                indexTree(data);
                const live = $el.data("dxTreeList") ? $el.dxTreeList("instance") : null;
                if (live) {
                    tree = live;
                    live.option({
                        dataSource: data,
                        expandedRowKeys: expandedKeysFrom(data),
                        autoExpandAll: false
                    });
                    setTimeout(syncCheckboxes, 0);
                    return;
                }
                tree = $el.dxTreeList({
                    dataSource: data,
                    keyExpr: "id",
                    parentIdExpr: "parentId",
                    rootValue: 0,
                    autoExpandAll: false,
                    expandedRowKeys: expandedKeysFrom(data),
                    width: "100%",
                    height: "100%",
                    showBorders: true,
                    showRowLines: true,
                    showColumnLines: false,
                    showTreeLines: false,
                    selection: { mode: "none" },
                    columns: [{
                        dataField: "name",
                        caption: caption,
                        cellTemplate: function (container, cellOptions) {
                            const wrap = $("<div class='" + cellClass + "'>");
                            const box = $("<input type='checkbox' class='" + checkClass + "'>").attr("data-key", cellOptions.key);
                            fillCheckbox(box[0], cellOptions.key);
                            wrap.append(box);
                            wrap.append($("<span class='" + nameClass + "'>").text(cellOptions.value || ""));
                            wrap.appendTo(container);
                        }
                    }],
                    scrolling: { mode: "virtual", useNative: false, showScrollbar: "always", scrollByContent: true, scrollByThumb: true },
                    paging: { enabled: false },
                    onRowPrepared: function (e) {
                        if (e.rowType === "data" && e.node && e.node.hasChildren)
                            e.rowElement.addClass("smc-parent-node");
                    },
                    onRowExpanded: function () { setTimeout(syncCheckboxes, 0); },
                    onRowCollapsed: function () { setTimeout(syncCheckboxes, 0); }
                }).dxTreeList("instance");
            }

            function catalogSig(data) {
                return (data || []).map(function (r) { return r.id + ":" + (r.name || ""); }).join("|");
            }

            function ensure(data, key) {
                data = data || [];
                const sig = (key || "") + "@" + catalogSig(data);
                if (tree && boundSig === sig) {
                    indexTree(data);
                    return;
                }
                boundKey = key;
                boundSig = sig;
                init(data);
            }

            return {
                init: init,
                ensure: ensure,
                applyGranted: applyGranted,
                collect: collect,
                isReady: function () { return ready; },
                updateDimensions: function () {
                    if (tree) {
                        tree.option("height", $el.height());
                        tree.updateDimensions();
                    }
                },
                dispose: function () {
                    if ($el.data("dxTreeList")) $el.dxTreeList("dispose");
                    $el.empty();
                    tree = null;
                    checkedSet = {};
                    childrenByParent = {};
                    boundKey = null;
                    boundSig = "";
                    ready = false;
                }
            };
        }
    };
})(window, jQuery);
