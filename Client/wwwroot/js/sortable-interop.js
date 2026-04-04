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
