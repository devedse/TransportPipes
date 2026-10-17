class SelectableObject {
    xPos: number;
    yPos: number;

    sprite: PIXI.Sprite;

    indexOfSelected: number = 0;
    indexOfHovered: number = 0;

    currentlyOwner: boolean = true;
    currentlySelected: boolean = false;
    currentlyHovered: boolean = false;

    static theImOwnerFilter: PIXI.filters.ColorMatrixFilter = null;
    static theSelectedFilter: PIXI.filters.ColorMatrixFilter = null;
    static theHoveredFilter: PIXI.filters.ColorMatrixFilter = null;

    //private static getImOwnerFilter() {
    //    if (SelectableObject.theImOwnerFilter == null) {
    //        SelectableObject.theImOwnerFilter = new PIXI.ColorMatrixFilter()

    //        var ownerMatrix: number[] = [
    //            1, 0, 0, 0.5,
    //            0, 1, 0, 0,
    //            0, 0, 1, 0,
    //            0, 0, 0, 1
    //        ];

    //        SelectableObject.theImOwnerFilter.matrix = ownerMatrix;
    //    }
    //    return SelectableObject.theImOwnerFilter;
    //}

    private static getSelectedFilter() {
        if (SelectableObject.theSelectedFilter == null) {
            SelectableObject.theSelectedFilter = new PIXI.filters.ColorMatrixFilter()


            //Need to fix transparancy here somehow...

            SelectableObject.theSelectedFilter.sepia(false);

            //var selectedMatrix: number[] = [
            //    1, 0, 0, 0, 0,
            //    0, 1, 0, 0, 0,
            //    0, 0, 1, 0, 0,
            //    0, 0, 0, 0.65, 0,
            //    0, 0, 0, 0, 1
            //];

            //SelectableObject.theSelectedFilter.matrix = selectedMatrix;
        }
        return SelectableObject.theSelectedFilter;
    }

    private static getHoveredFilter() {
        if (SelectableObject.theHoveredFilter == null) {
            SelectableObject.theHoveredFilter = new PIXI.filters.ColorMatrixFilter()

            SelectableObject.theHoveredFilter.brightness(2, false);
        }
        return SelectableObject.theHoveredFilter;
    }


    constructor(sprite: PIXI.Sprite) {
        this.sprite = sprite;
    }

    setOwner(owner: boolean) {
        if (owner != this.currentlyOwner) {
            if (owner) {
                this.currentlyOwner = true;
                this.sprite.tint = 0xFFFFFF;
            }
            else {
                this.currentlyOwner = false;
                this.sprite.tint = 0x886666;
            }
        }

        //if (owner != this.currentlyOwner) {
        //    if (owner) {
        //        this.currentlyOwner = true;
        //    }
        //    else {
        //        this.currentlyOwner = false;
        //    }
        //    this.setFilters();
        //}
    }

    setSelected(selected: boolean) {
        if (selected != this.currentlySelected) {
            if (selected) {
                this.currentlySelected = true;
            }
            else {
                this.currentlySelected = false;
            }
            this.setFilters();
        }
    }

    setHovered(hovered: boolean) {
        if (hovered != this.currentlyHovered) {
            if (hovered) {
                this.currentlyHovered = true;
            }
            else {
                this.currentlyHovered = false;
            }
            this.setFilters();
        }
    }

    setFilters() {
        if (this.currentlyHovered || this.currentlySelected) { //|| this.currentlyOwner
            var filterstoset: PIXI.filters.ColorMatrixFilter[] = [];

            if (this.currentlySelected) {
                filterstoset.push(SelectableObject.getSelectedFilter());
            }
            if (this.currentlyHovered) {
                filterstoset.push(SelectableObject.getHoveredFilter());
            }
            //if (this.currentlyOwner) {
            //    filterstoset.push(SelectableObject.getImOwnerFilter());
            //}

            this.sprite.filters = filterstoset;
        }
        else {
            this.sprite.filters = null;
        }
    }
} 