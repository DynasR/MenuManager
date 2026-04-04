window.initSortable = function (element, dotNetRef, group) {
    if (element._sortableInstance) {
        element._sortableInstance.destroy();
    }

    element._sortableInstance = new Sortable(element, {
        group: group,
        animation: 150,
        onEnd: function (evt) {
            var fromListId = evt.from.dataset.listId;
            var toListId = evt.to.dataset.listId;
            var itemId = parseInt(evt.item.dataset.itemId);
            dotNetRef.invokeMethodAsync("OnItemMoved", fromListId, toListId, itemId);
        }
    });
};

window.destroySortable = function (element) {
    if (element._sortableInstance) {
        element._sortableInstance.destroy();
        delete element._sortableInstance;
    }
};
