window.initSortable = function (element, dotNetRef, group) {
    if (element._sortableInstance) {
        element._sortableInstance.destroy();
    }

    element._sortableInstance = new Sortable(element, {
        group: group,
        animation: 150,
        onEnd: function (evt) {
            var itemId = parseInt(evt.item.dataset.itemId);
            var fromDate = evt.from.dataset.date;
            var fromMealType = evt.from.dataset.mealtype;
            var toDate = evt.to.dataset.date;
            var toMealType = evt.to.dataset.mealtype;
            var newIndex = evt.newIndex;
            dotNetRef.invokeMethodAsync("OnDrop", itemId, fromDate, fromMealType, toDate, toMealType, newIndex);
        }
    });
};

window.destroySortable = function (element) {
    if (element._sortableInstance) {
        element._sortableInstance.destroy();
        delete element._sortableInstance;
    }
};
