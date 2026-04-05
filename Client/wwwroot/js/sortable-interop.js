(function () {
    var registry = new Map();

    window.initSortable = function (element, group) {
        // Destroy existing instance if any — one instance per element at all times
        var existing = registry.get(element);
        if (existing) {
            existing.sortable.destroy();
            if (existing.moveHandler)
                element.removeEventListener("sortmove", existing.moveHandler);
            if (existing.reorderHandler)
                element.removeEventListener("sortreorder", existing.reorderHandler);
            registry.delete(element);
        }

        var sortable = new Sortable(element, {
            group: group,
            animation: 150,
            onEnd: function (evt) {
                evt.from.dispatchEvent(new CustomEvent("sortcomplete", {
                    detail: {
                        isCrossCell: evt.from !== evt.to,
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
                dotNetRef.invokeMethodAsync("OnDrop", d.itemId, d.fromDate, d.fromMealType, d.toDate, d.toMealType, d.newIndex);
            } else {
                var ids = Array.from(element.children).map(function (li) {
                    return parseInt(li.dataset.itemId);
                });
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

    // Footer drag-and-drop source storage — { element, date, mealType, isCopy, _keyDown, _keyUp }
    var footerDragSource = null;

    function clearFooterDragSource() {
        if (!footerDragSource) return;
        if (footerDragSource.element) {
            footerDragSource.element.classList.remove('footer-drag-active', 'footer-copy-mode');
            footerDragSource.element.removeEventListener('dragend', footerDragSource._dragEnd);
        }
        document.removeEventListener('keydown', footerDragSource._keyDown);
        document.removeEventListener('keyup', footerDragSource._keyUp);
        footerDragSource = null;
    }

    window.setFooterDragSource = function (element, date, mealType, isCopy) {
        clearFooterDragSource();

        var src = { element: element, date: date, mealType: mealType, isCopy: isCopy };

        src._keyDown = function (e) {
            if (e.key === 'Control' && footerDragSource) {
                footerDragSource.isCopy = true;
                footerDragSource.element.classList.add('footer-copy-mode');
            }
        };
        src._keyUp = function (e) {
            if (e.key === 'Control' && footerDragSource) {
                footerDragSource.isCopy = false;
                footerDragSource.element.classList.remove('footer-copy-mode');
            }
        };
        src._dragEnd = function () { clearFooterDragSource(); };

        element.classList.add('footer-drag-active');
        if (isCopy) element.classList.add('footer-copy-mode');

        document.addEventListener('keydown', src._keyDown);
        document.addEventListener('keyup', src._keyUp);
        element.addEventListener('dragend', src._dragEnd);

        footerDragSource = src;
    };

    window.getAndClearFooterDragSource = function () {
        if (!footerDragSource) return null;
        var result = footerDragSource.date + '|' + footerDragSource.mealType + '|' + (footerDragSource.isCopy ? 'true' : 'false');
        clearFooterDragSource();
        return result;
    };

    // Synchronous dragover handler + copy/move visual indicator via CSS classes
    window.addCellDragOverHandler = function (element) {
        function dragoverHandler(e) {
            if (footerDragSource) e.preventDefault();
        }

        function dragenterHandler(e) {
            if (!footerDragSource) return;
            if (element.contains(e.relatedTarget)) return;
            element.classList.remove('meal-cell-drag-copy', 'meal-cell-drag-move');
            element.classList.add(footerDragSource.isCopy ? 'meal-cell-drag-copy' : 'meal-cell-drag-move');
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

    // Global dragover handler for the trash zone — Blazor's @ondragover:preventDefault
    // is not reliably synchronous in WASM; we need a native JS listener.
    document.addEventListener('dragover', function (e) {
        if (footerDragSource && e.target.closest('.dayplan-delete-zone')) {
            e.preventDefault();
        }
    });

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
