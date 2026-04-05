(function () {
    var registry = new Map();

    // --- SortableJS item drag state ---
    var _sortableDragItem = null;
    var _sortableDragFrom = null;
    var _sortableCopyGhost = null;

    function showDragGhost() {
        if (!_sortableDragFrom || !_sortableDragItem || _sortableCopyGhost) return;
        var clone = _sortableDragItem.cloneNode(true);
        clone.classList.add('sortable-copy-ghost');
        clone.removeAttribute('data-item-id'); // exclude from OnReorder id list
        _sortableDragFrom.appendChild(clone);
        _sortableCopyGhost = clone;
    }

    function hideDragGhost() {
        if (_sortableCopyGhost) {
            _sortableCopyGhost.remove();
            _sortableCopyGhost = null;
        }
    }

    function clearSortableState() {
        hideDragGhost();
        _sortableDragItem = null;
        _sortableDragFrom = null;
    }

    window.initSortable = function (element, group) {
        // Destroy existing instance if any — one instance per element at all times
        var existing = registry.get(element);
        if (existing) {
            existing.sortable.destroy();
            if (existing.moveHandler)
                element.removeEventListener("sortcomplete", existing.moveHandler);
            registry.delete(element);
        }

        var sortable = new Sortable(element, {
            group: group,
            animation: 150,
            filter: '.sortable-copy-ghost', // ghost is not draggable
            onStart: function (evt) {
                _sortableDragItem = evt.item;
                _sortableDragFrom = evt.from;
                // Ghost appears immediately at dragstart and never moves until dragend
                showDragGhost();
            },
            onEnd: function (evt) {
                // ctrlKey read at drop time — single source of truth for copy vs move
                var isCopy = !!(evt.originalEvent && evt.originalEvent.ctrlKey);
                clearSortableState(); // removes ghost before we count siblings

                if (isCopy && evt.from !== evt.to) {
                    // Copy: SortableJS moved the element to target — put it back in source
                    // at its original index so Blazor's DOM stays consistent.
                    var siblings = Array.from(evt.from.children);
                    var refChild = siblings[evt.oldIndex] || null;
                    evt.from.insertBefore(evt.item, refChild);
                }

                evt.from.dispatchEvent(new CustomEvent("sortcomplete", {
                    detail: {
                        isCrossCell: evt.from !== evt.to,
                        isCopy: isCopy,
                        itemId: parseInt(evt.item.dataset.itemId),
                        fromDate: evt.from.dataset.date,
                        fromMealType: evt.from.dataset.mealtype,
                        toDate: evt.to.dataset.date,
                        toMealType: evt.to.dataset.mealtype,
                        newIndex: evt.newIndex
                    }
                }));
            }
        });

        registry.set(element, { sortable: sortable });
    };

    window.observeSortable = function (element, dotNetRef) {
        var entry = registry.get(element);

        function moveHandler(e) {
            var d = e.detail;
            if (d.isCrossCell) {
                dotNetRef.invokeMethodAsync("OnDrop", d.itemId, d.fromDate, d.fromMealType, d.toDate, d.toMealType, d.newIndex, d.isCopy === true);
            } else {
                // Filter out the copy ghost (no data-item-id) before sending ids
                var ids = Array.from(element.children)
                    .filter(function (li) { return li.dataset.itemId; })
                    .map(function (li) { return parseInt(li.dataset.itemId); });
                dotNetRef.invokeMethodAsync("OnReorder", ids);
            }
        }

        element.addEventListener("sortcomplete", moveHandler);

        if (entry) {
            entry.moveHandler = moveHandler;
            entry.dotNetRef = dotNetRef;
        } else {
            registry.set(element, { moveHandler: moveHandler, dotNetRef: dotNetRef });
        }
    };

    // --- Footer cell drag-and-drop (zone latérale → copie/déplace cellule entière) ---
    var footerDragSource = null;

    function clearFooterDragSource() {
        if (!footerDragSource) return;
        document.removeEventListener('keydown', footerDragSource._keyDown);
        document.removeEventListener('keyup', footerDragSource._keyUp);
        document.removeEventListener('dragover', footerDragSource._dragOver);
        footerDragSource = null;
    }
    window.clearFooterDragSource = clearFooterDragSource;

    window.setFooterDragSource = function (element, date, mealType, initialCopy) {
        clearFooterDragSource();
        // element = .meal-cell-footer; parentElement = .meal-cell
        var cell = element.parentElement;
        var src = {
            date: date,
            mealType: mealType,
            isCopy: !!initialCopy,
            cell: cell,
            sourceList: cell ? cell.querySelector('.meal-cell-list') : null
        };
        src._keyDown = function (e) {
            if (e.key === 'Control' && footerDragSource) {
                footerDragSource.isCopy = true;
                footerDragSource.cell.classList.add('cell-drag-copy-mode');
            }
        };
        src._keyUp = function (e) {
            if (e.key === 'Control' && footerDragSource) {
                footerDragSource.isCopy = false;
                footerDragSource.cell.classList.remove('cell-drag-copy-mode');
            }
        };
        src._dragOver = function (e) {
            if (footerDragSource) {
                footerDragSource.isCopy = !!e.ctrlKey;
                footerDragSource.cell.classList.toggle('cell-drag-copy-mode', !!e.ctrlKey);
            }
        };
        document.addEventListener('keydown', src._keyDown);
        document.addEventListener('keyup', src._keyUp);
        document.addEventListener('dragover', src._dragOver);
        footerDragSource = src;
    };

    window.getAndClearFooterDragSource = function () {
        if (!footerDragSource) return null;
        var result = footerDragSource.date + '|' + footerDragSource.mealType + '|' + (footerDragSource.isCopy ? '1' : '0');
        clearFooterDragSource();
        return result;
    };

    // --- Cell dragover handlers (footer drag + sortable copy/move visual) ---
    window.addCellDragOverHandler = function (element) {
        function dragoverHandler(e) {
            if (footerDragSource) { e.preventDefault(); return; }
            if (_sortableDragItem) {
                // Live copy/move visual on target cell — does not affect the ghost
                element.classList.remove('meal-cell-drag-copy', 'meal-cell-drag-move');
                element.classList.add(e.ctrlKey ? 'meal-cell-drag-copy' : 'meal-cell-drag-move');
            }
        }

        function dragenterHandler(e) {
            if (element.contains(e.relatedTarget)) return;
            if (footerDragSource) {
                if (element === footerDragSource.cell) return;
                element.classList.remove('meal-cell-drag-copy', 'meal-cell-drag-move');
                element.classList.add('meal-cell-drag-move');
            } else if (_sortableDragItem) {
                element.classList.remove('meal-cell-drag-copy', 'meal-cell-drag-move');
                element.classList.add(e.ctrlKey ? 'meal-cell-drag-copy' : 'meal-cell-drag-move');
            }
        }

        function dragleaveHandler(e) {
            if (element.contains(e.relatedTarget)) return;
            element.classList.remove('meal-cell-drag-copy', 'meal-cell-drag-move');
        }

        function dropHandler() {
            element.classList.remove('meal-cell-drag-copy', 'meal-cell-drag-move');
        }

        element.__cellDragOver = dragoverHandler;
        element.__cellDragEnter = dragenterHandler;
        element.__cellDragLeave = dragleaveHandler;
        element.__cellDrop = dropHandler;

        element.addEventListener('dragover', dragoverHandler);
        element.addEventListener('dragenter', dragenterHandler);
        element.addEventListener('dragleave', dragleaveHandler);
        element.addEventListener('drop', dropHandler);
    };

    window.removeCellDragOverHandler = function (element) {
        if (!element) return;
        if (element.__cellDragOver) { element.removeEventListener('dragover', element.__cellDragOver); delete element.__cellDragOver; }
        if (element.__cellDragEnter) { element.removeEventListener('dragenter', element.__cellDragEnter); delete element.__cellDragEnter; }
        if (element.__cellDragLeave) { element.removeEventListener('dragleave', element.__cellDragLeave); delete element.__cellDragLeave; }
        if (element.__cellDrop) { element.removeEventListener('drop', element.__cellDrop); delete element.__cellDrop; }
    };

    // --- Primed axis overlay (single overlay covering entire row or column) ---
    var _primedOverlay = null;

    function clearPrimedOverlay() {
        if (_primedOverlay) {
            _primedOverlay.remove();
            _primedOverlay = null;
        }
    }

    function createAxisOverlay(grid, elements) {
        clearPrimedOverlay();
        if (!elements.length) return;
        var top = Infinity, left = Infinity, bottom = -Infinity, right = -Infinity;
        elements.forEach(function (el) {
            var t = el.offsetTop;
            var l = el.offsetLeft;
            top    = Math.min(top,    t);
            left   = Math.min(left,   l);
            bottom = Math.max(bottom, t + el.offsetHeight);
            right  = Math.max(right,  l + el.offsetWidth);
        });
        if (top === Infinity) return;
        var overlay = document.createElement('div');
        overlay.className = 'primed-axis-overlay';
        overlay.style.top    = top + 'px';
        overlay.style.left   = left + 'px';
        overlay.style.width  = (right - left) + 'px';
        overlay.style.height = (bottom - top) + 'px';
        grid.appendChild(overlay);
        _primedOverlay = overlay;
    }

    // --- Row primed (mousedown on date cell) ---
    window.addRowPrimed = function (date) {
        var grid = document.querySelector('.dayplan-grid');
        if (!grid) return;
        createAxisOverlay(grid, Array.from(document.querySelectorAll('[data-rowdate="' + date + '"]')));
    };

    window.removeRowPrimed = function (date) {
        clearPrimedOverlay();
    };

    // --- Column primed (mousedown on MealType header) ---
    window.addColumnPrimed = function (mealType) {
        var grid = document.querySelector('.dayplan-grid');
        if (!grid) return;
        createAxisOverlay(grid, Array.from(document.querySelectorAll('[data-colmealtype="' + mealType + '"]')));
    };

    window.removeColumnPrimed = function (mealType) {
        clearPrimedOverlay();
    };

    window.destroySortable = function (element) {
        var entry = registry.get(element);
        if (!entry) return;

        if (entry.sortable) entry.sortable.destroy();
        if (entry.moveHandler)
            element.removeEventListener("sortcomplete", entry.moveHandler);
        if (entry.dotNetRef) entry.dotNetRef = null;

        registry.delete(element);
    };
})();
