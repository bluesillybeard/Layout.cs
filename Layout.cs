//inspired by a C header: https://github.com/randrew/layout
// I was originally going to directly port it to C#,
// but it has a lot of strangeness and C stuff that I don't like.
// So, I made my own version.
using LayoutNumber = System.Int16;
using ItemRef = System.Int32;
using System.Security.Cryptography;

public struct LayoutVec2
{
    public LayoutVec2(LayoutNumber x, LayoutNumber y)
    {
        this.x = x;
        this.y = y;
    }
    public LayoutNumber x, y;
}

public struct LayoutVec4
{
    public LayoutVec4(LayoutNumber x, LayoutNumber y, LayoutNumber z, LayoutNumber w)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.w = w;
    }
    public LayoutNumber x, y, z, w;
}

public struct ItemFlags
{
    /*
    This is a bit field.
    //properties that apply to children of this item
    1:the direction children of this item will be stacked; 0->horizontal, 1->vertical
    2:how to allign the children. 0->start, 1->center, 2->end
    1:weather children should "wrap" to a new row/column when they go off the edge.
    1:weather children should be expanded to fill the entire container in its stacking direction
    //properties that apply to this item
    1:if 1, the this will expand to match the size in the direction 
        perpendicular to the stacking direction of its parent
        If the parents stacking direction is vertical, then the item fills the horizontal direction.
    2:how to allign this item in the direction perpendicular to the stacking direction of its parent.
        0->start, 1->center 2->end
    1: if true and this item is in a wrapping container, 
        then it will wrap on this item instead of at the edge.
    //internal housekeeping stuff
    1:false if the item is removed
    1:keeps track of which items are going to wrap.

    */
    public uint flags;
    //This mess calculates all of the bit masks at compile time
    // https://stackoverflow.com/questions/14464/bit-fields-in-c-sharp
    const int SDS = 1, SDL = 0,           SDM = ((1 << SDS) - 1) << SDL;//stack direction
    const int AS = 2,  AL  = SDL + SDS,   AM  = ((1 << AS ) - 1) << AL ;//allignment
    const int WS = 1,  WL  = AS  + AL ,   WM  = ((1 << WS ) - 1) << WL ;//wrap
    const int FS = 1,  FL  = WS  + WL ,   FM  = ((1 << FS ) - 1) << FL ;//fill
    const int ES = 1,  EL  = FS  + FL ,   EM  = ((1 << ES ) - 1) << EL ;//expand
    const int PAS = 2, PAL = ES  + EL ,   PAM = ((1 << PAS) - 1) << PAL;//perpendicular allignment
    const int BS  = 1, BL  = PAS + PAL,   BM  = ((1 << NRS) - 1) << BL ; //break
    const int NRS = 1, NRL = BS + BL,     NRM = ((1 << NRS) - 1) << NRL; //not removed
    const uint Horizontal = 0;
    const uint Vertical = 1;
    ///<summary>0->horizontal, 1->vertical</summary>
    public uint StackDirection {
        get { return (uint)(flags & SDM) >> SDL; }
        set { flags = (uint)(flags & ~SDM | (value << SDL) & SDM); }
    }
    ///<summary>0->start, 1->center, 2->end</summary>
    public uint Allignment {
        get { return (uint)(flags & AM) >> AL; }
        set { flags = (uint)(flags & ~AM | (value << AL) & AM); }
    }
    ///<summary>0->no wrap, 1->wrap</summary>
    public uint Wrap {
        get { return (uint)(flags & WM) >> WL; }
        set { flags = (uint)(flags & ~WM | (value << WL) & WM); }
    }
    ///<summary>0->no fill, 1->fill</summary>
    public uint Fill {
        get { return (uint)(flags & FM) >> FL; }
        set { flags = (uint)(flags & ~FM | (value << FL) & FM); }
    }
    ///<summary>0->no expand, 1->expand</summary>
    public uint Expand {
        get { return (uint)(flags & EM) >> EL; }
        set { flags = (uint)(flags & ~EM | (value << EL) & EM); }
    }
    ///<summary>0->start, 1->center, 2->end</summary>
    public uint PerpendicularAllignment {
        get { return (uint)(flags & PAM) >> PAL; }
        set { flags = (uint)(flags & ~PAM | (value << PAL) & PAM); }
    }
    ///<summary>0->no break, 1->break</summary>
    public uint Break {
        get { return (uint)(flags & BM) >> BL; }
        set { flags = (uint)(flags & ~BM | (value << BL) & BM); }
    }
    ///<summary>0->not in tree, 1->in tree</summary>
    public uint InTree {
        get { return (uint)(flags & NRM) >> NRL; }
        set { flags = (uint)(flags & ~NRM | (value << NRL) & NRM); }
    }

}
struct Item
{
    public Item()
    {
        flags = new ItemFlags();
        parent = -1;
        firstChild = -1;
        nextSibling = -1;
        previousSibling = -1;
        minSize = new LayoutVec2();
        maxSize = new LayoutVec2();
        finalRect = new LayoutVec4();
        margin = new LayoutVec4();
    }
    public ItemFlags flags;
    public ItemRef parent;
    public ItemRef firstChild;
    public ItemRef nextSibling;
    public ItemRef previousSibling;
    public LayoutVec2 minSize;
    public LayoutVec2 maxSize;
    //x=left, y=top, z=width, w=height,
    public LayoutVec4 finalRect;
    //x=left, y=top, z=right, w=bottom
    public LayoutVec4 margin;

}

//TODO: test
public class Layout
{
    //This is a sparse list - 
    // Some items are initialized and valid,
    // but they are considered empty.
    // empty items are held in a separate list.
    //Items are referenced by their index in the list,
    // and the index must stay the same, so items that are removed are simply marked as empty.
    List<Item> items;
    Stack<ItemRef> clearItems;

    public Layout(int initialCapacity)
    {
        items = new List<Item>(initialCapacity);
        clearItems = new Stack<ItemRef>(initialCapacity);
    }
    public Layout()
    {
        items = new List<Item>();
        clearItems = new Stack<ItemRef>();
        InitRoot();
    }

    private void InitRoot()
    {
        Item root = new Item();
        //The default value for InTree is false.
        root.flags.InTree = 1;
        items.Add(root);
    }

    //methods that build the layout
    public ItemRef CreateChild(ItemRef parent)
    {
        if(!clearItems.TryPop(out ItemRef child))
        {
            child = (ItemRef)items.Count;
            items.Add(new Item());
        }
        AddChild(parent, child);
        Item item = new Item();
        item.flags.InTree = 1;
        items[child] = item;
        return child;
    }

    public ItemRef CreateSibling(ItemRef item)
    {
        if(!clearItems.TryPop(out ItemRef sibling))
        {
            sibling = (ItemRef)items.Count;
            items.Add(new Item());
        }
        AddSibling(items[item].parent, item, sibling);
        Item itemItem = new Item();
        itemItem.flags.InTree = 1;
        items[sibling] = itemItem;
        return sibling;
    }

    public void SetItemFlags(ItemRef itemRef, ItemFlags flags)
    {
        Item item = items[itemRef];
        item.flags = flags;
        items[itemRef] = item;
    }
    public void SetItemMinSize(ItemRef itemRef, LayoutVec2 minSize)
    {
        Item item = items[itemRef];
        item.minSize = minSize;
        items[itemRef] = item;
    }
    public void SetItemMaxSize(ItemRef itemRef, LayoutVec2 maxSize)
    {
        Item item = items[itemRef];
        item.maxSize = maxSize;
        items[itemRef] = item;
    }
    public void SetItemMargin(ItemRef itemRef, LayoutVec4 margin)
    {
        Item item = items[itemRef];
        item.margin = margin;
        items[itemRef] = item;
    }
    public void SetItem(ItemRef itemRef, ItemFlags flags, LayoutVec2 minSize, LayoutVec2 maxSize, LayoutVec4 margin)
    {
        Item item = items[itemRef];
        item.flags = flags;
        item.minSize = minSize;
        item.maxSize = maxSize;
        item.margin = margin;
        items[itemRef] = item;
    }

    public ItemFlags GetItemFlags(ItemRef item)
    {
        return items[item].flags;
    }
    public ItemRef GetItemParent(ItemRef item)
    {
        return items[item].parent;
    }

    public ItemRef GetItemFirstChild(ItemRef item)
    {
        return items[item].firstChild;
    }
    public ItemRef GetItemNextSibling(ItemRef item)
    {
        return items[item].nextSibling;
    }
    public ItemRef GetItemPreviousSibling(ItemRef item)
    {
        return items[item].previousSibling;
    }

    public LayoutVec2 GetItemMinSize(ItemRef item)
    {
        return items[item].minSize;
    }
    public LayoutVec2 GetItemMaxSize(ItemRef item)
    {
        return items[item].maxSize;
    }
    public LayoutVec4 GetItemFinalRect(ItemRef item)
    {
        return items[item].finalRect;
    }
    public LayoutVec4 GetItemMargin(ItemRef item)
    {
        return items[item].margin;
    }
    //removes an item and its entire subtree.
    public void Remove(ItemRef item)
    {
        if(item == 0)
            throw new Exception("Cannot remove the root node!");
        Item itemItem = items[item];
        // This recursive function doesn't actually remove anything,
        // it just marks all of the tree as being removed.
        if(itemItem.firstChild != -1)
            RemoveInternal(itemItem.firstChild);
        //Unlike the internal remove function,
        // this one has to actually remove references
        // and set the tree back to being valid
        Item parentItem = items[itemItem.parent];
        if(parentItem.firstChild == item)
        {
            // this is the first child
            parentItem.firstChild = itemItem.nextSibling;
            if(itemItem.nextSibling != -1)
            {
                //this does have a sibling
                Item sibling = items[itemItem.nextSibling];
                sibling.previousSibling = -1;
                items[itemItem.nextSibling] = sibling;
            }
            items[itemItem.parent] = parentItem;
        }
        else
        {
            //we aren't the first child
            Item previousSibling = items[itemItem.previousSibling];
            previousSibling.nextSibling = itemItem.nextSibling;
            if(itemItem.nextSibling != -1)
            {
                // this isn't the last one
                Item nextSibling = items[itemItem.nextSibling];
                nextSibling.previousSibling = itemItem.previousSibling;
                items[itemItem.nextSibling] = nextSibling;
            }
            items[itemItem.previousSibling] = previousSibling;
        }
        markAsRemoved(itemItem, item);
    }
    public void Clear()
    {
        items.Clear();
        clearItems.Clear();
        InitRoot();
    }
    void RemoveInternal(ItemRef item)
    {
        Item itemItem = items[item];
        //First, remove the child (depth first)
        if(itemItem.firstChild != -1)
            RemoveInternal(itemItem.firstChild);
        //We also need to remove this nodes siblings.
        if(itemItem.nextSibling != -1)
            RemoveInternal(itemItem.nextSibling);
        markAsRemoved(itemItem, item);
        // Removing references from this is not required
        // because all of its siblings are being removed anyway,
        // and the parent has already removed its reference to this node.
        // clearing the data is also not required since that's done when an item is created.
    }

    void markAsRemoved(Item item, ItemRef itemRef)
    {
        //if this is the last item, just shrink the list
        if(itemRef == items.Count-1)
        {
            items.RemoveAt(itemRef);
            return;
        }
        item.flags.InTree = 0;
        items[itemRef] = item;
        clearItems.Push(itemRef);
    }

    void AddChild(ItemRef parent, ItemRef newChild)
    {
        Item parentItem = items[parent];
        if(parentItem.firstChild == -1)
        {
            parentItem.firstChild = newChild;
            items[parent] = parentItem;
            return;
        }
        AddSibling(parent, parentItem.firstChild, newChild);
    }

    void AddSibling(ItemRef parent, ItemRef item, ItemRef newSibling)
    {
        ItemRef iteration = item;
        for(;;)
        {
            Item iterationItem = items[iteration];
            if(iterationItem.nextSibling == -1)
            {
                iterationItem.nextSibling = newSibling;
                items[iteration] = iterationItem;
                return;
            }
            iteration = iterationItem.nextSibling;
        }
    }
    //when the items are iterated:
    // Their sizes are determined first, depth first iteration.
    // Then they are positioned, also depth first iteration.

    //Iterates the entire GUI tree
    // and determines every elements bounds
    public void DetermineLayout(LayoutVec2 windowSize)
    {
        DetermineSizeMinimums(0);
        //DetermineSizes only works on the item's children,
        // So we also need to do the root node
        Item root = items[0];
        root.finalRect.z = windowSize.x;
        root.finalRect.w = windowSize.y;
        items[0] = root;
        DetermineSizes(0, windowSize);
        DeterminePositions(0, new LayoutVec2(0, 0));
    }

    //This only figures out the smallest size that the children must be.
    //TODO: I might be able to incorperate this function into the DetermineSizes function,
    // Only do that AFTER they are both complete however
    // because the maxSize and wrap parameters will make the DetermineSizes function a lot more complex
    void DetermineSizeMinimums(ItemRef itemRef)
    {
        Item item = items[itemRef];
        //iterate the tree recursively depth first
        if(item.firstChild != -1)
            DetermineSizeMinimums(item.firstChild);
        if(item.nextSibling != -1)
            DetermineSizeMinimums(item.nextSibling);
        
        LayoutVec2 size = new LayoutVec2();
        //If there are no children, then the minimum size is super simple
        if(item.firstChild == -1)
        {
            size = item.minSize;
        }
        //Non-wrapping
        else if(item.flags.Wrap == 0)
        {
            //TODO: there has to be a way to deduplicate this code in a useful way
            //add the sizes of the children
            if(item.flags.StackDirection == 0)
            {
                //horizontal stack direction
                // total item sizes in the stacking direction,
                // find the largest element in the perpendicular direction
                ItemRef iterator = item.firstChild;
                while(iterator != -1)
                {
                    Item iteratorItem = items[iterator];
                    //the item's "min size" is what's defined by the user of the library.
                    // the min size we need to use here is the calculated min size,
                    // which was conveniently written to the finalRect when the children were iterated.
                    // But, we also add the margin to that size, since the container also contains its childrens margins
                    LayoutNumber itemMinSizeX = (LayoutNumber)(iteratorItem.finalRect.z + iteratorItem.margin.x + iteratorItem.margin.z);
                    LayoutNumber itemMinSizeY = (LayoutNumber)(iteratorItem.finalRect.w + iteratorItem.margin.y + iteratorItem.margin.w);
                    size.x += itemMinSizeX;
                    if(itemMinSizeY > size.y)
                    {
                        size.y = itemMinSizeY;
                    }
                    iterator = iteratorItem.nextSibling;
                }
            }
            else
            {
                //vertial stack direction
                // total item sizes in the stacking direction,
                // find the largest element in the perpendicular direction
                ItemRef iterator = item.firstChild;
                while(iterator != -1)
                {
                    Item iteratorItem = items[iterator];
                    //the item's "min size" is what's defined by the user of the library.
                    // the min size we need to use here is the calculated min size,
                    // which was conveniently written to the finalRect when the children were iterated.
                    // But, we also add the margin to that size, since the container also contains its childrens margins
                    LayoutNumber itemMinSizeX = (LayoutNumber)(iteratorItem.finalRect.z + iteratorItem.margin.x + iteratorItem.margin.z);
                    LayoutNumber itemMinSizeY = (LayoutNumber)(iteratorItem.finalRect.w + iteratorItem.margin.y + iteratorItem.margin.w);
                    size.y += itemMinSizeY;
                    if(itemMinSizeX > size.x)
                    {
                        size.x = itemMinSizeX;
                    }
                    iterator = iteratorItem.nextSibling;
                }
            }
        }
        else
        {
            //Wrap is really annoying because we really have NO IDEA how big this is going to be.
            // the size depends on how much space is available, so the 'minimum size' is completely pointless.
            // In an attempt to make it vaguely useful, the 'minimum' size is the largest elements.
            ItemRef iterator = item.firstChild;
            while(iterator != -1)
            {
                Item iteratorItem = items[iterator];
                //the item's "min size" is what's defined by the user of the library.
                // the min size we need to use here is the calculated min size,
                // which was conveniently written to the finalRect when the children were iterated.
                // But, we also add the margin to that size, since the container also contains its childrens margins
                LayoutNumber itemMinSizeX = (LayoutNumber)(iteratorItem.finalRect.z + iteratorItem.margin.x + iteratorItem.margin.z);
                LayoutNumber itemMinSizeY = (LayoutNumber)(iteratorItem.finalRect.w + iteratorItem.margin.y + iteratorItem.margin.w);
                if(itemMinSizeX > size.x)
                {
                    size.x = itemMinSizeX;
                }
                if(itemMinSizeY > size.y)
                {
                    size.y = itemMinSizeY;
                }
                iterator = iteratorItem.nextSibling;
            }
        }
        //set the "final" rect output to be our final size
        // This will be used in the final determineSizes function later.
        item.finalRect = new LayoutVec4(0, 0, size.x, size.y);
        items[itemRef] = item;
    }

    void DetermineSizes(ItemRef itemRef, LayoutVec2 maxSize)
    {
        Item item = items[itemRef];
        //Figure out the children's sizes
        // The size depends on a lot of things, which is really annoying
        // However, for now it depends exclusively this fill value and the childs expand.
        // TODO: account for child maxSize, Wrap

        ItemRef iterator = item.firstChild;
        // Total our number of children
        int numberOfChildren = 0;
        while(iterator != -1)
        {
            Item child = items[iterator];
            iterator = child.nextSibling;
            numberOfChildren++;
        }
        iterator = item.firstChild;
        //For every child
        while(iterator != -1)
        {
            Item child = items[iterator];
            LayoutVec2 childMarginTotal = new LayoutVec2(
                (LayoutNumber)(child.margin.x + child.margin.z),
                (LayoutNumber)(child.margin.y + child.margin.w)
            );
            
            //this variable also includes the margin, which will need to be subtracted later.
            LayoutVec2 childSize = new LayoutVec2();
            //REMEMBER: fill is in the stacking direction, expand is in the perpendicular direction
            //TODO: lots of deduplication potential in this one
            // TODO: account for child maxSize, Wrap
            switch(child.flags.Expand*4 + item.flags.Fill*2 + item.flags.StackDirection)
            {
                case 0b000:
                    //child expand = 0, fill=0, horizontal
                    childSize.x = (LayoutNumber)(child.finalRect.z + childMarginTotal.x);
                    childSize.y = (LayoutNumber)(child.finalRect.w + childMarginTotal.y);
                    break;
                case 0b001:
                    //child expand = 0, fill=0, vertical
                    childSize.x = (LayoutNumber)(child.finalRect.z + childMarginTotal.x);
                    childSize.y = (LayoutNumber)(child.finalRect.w + childMarginTotal.y);
                    break;
                case 0b010:
                    //child expand = 0, fill=1, horizontal
                    childSize.x = (LayoutNumber)(maxSize.x/numberOfChildren);
                    childSize.y = (LayoutNumber)(child.finalRect.w + childMarginTotal.y);
                    break;
                case 0b011:
                    //child expand = 0, fill=1, vertical
                    childSize.x = (LayoutNumber)(child.finalRect.z + childMarginTotal.x);
                    childSize.y = (LayoutNumber)(maxSize.y/numberOfChildren);
                    break;
                case 0b100:
                    //child expand = 1, fill=0, horizontal
                    childSize.x = (LayoutNumber)(child.finalRect.z + childMarginTotal.x);
                    childSize.y = maxSize.y;
                    break;
                case 0b101:
                    //child expand = 1, fill=0, vertical
                    childSize.x = maxSize.x;
                    childSize.y = (LayoutNumber)(child.finalRect.w + childMarginTotal.y);
                    break;
                case 0b110:
                    //child expand = 1, fill=1, horizontal
                    childSize.x = (LayoutNumber)(maxSize.x/numberOfChildren);
                    childSize.y = maxSize.y;
                    break;
                case 0b111:
                    //child expand = 1, fill=1, vertical
                    childSize.x = maxSize.x;
                    childSize.y = (LayoutNumber)(maxSize.y/numberOfChildren);
                    break;
            }
            childSize.x = (LayoutNumber)(childSize.x - childMarginTotal.x);
            childSize.y = (LayoutNumber)(childSize.y - childMarginTotal.y);
            child.finalRect.z = childSize.x;
            child.finalRect.w = childSize.y;
            items[iterator] = child;
            DetermineSizes(iterator, childSize);
            iterator = child.nextSibling;
        }
    }
    //like DetermineSizes, this determines the positions of the children of the item
    // given its position
    //TODO: allignment, wrap
    void DeterminePositions(ItemRef itemRef, LayoutVec2 pos)
    {
        Item item = items[itemRef];
        ItemRef childRef = item.firstChild;
        LayoutVec2 childPos = new LayoutVec2();
        switch(item.flags.Allignment)
        {
            case 1:
                while(childRef != -1)
                {
                    Item child = items[childRef];
                    childPos.x += (LayoutNumber)(child.finalRect.z + child.margin.x + child.margin.z);
                    childPos.y += (LayoutNumber)(child.finalRect.w + child.margin.y + child.margin.w);
                    childRef = child.nextSibling;
                }
                childPos.x = (LayoutNumber)(item.finalRect.z/2 - childPos.x/2);
                childPos.y = (LayoutNumber)(item.finalRect.w/2 - childPos.y/2);
                break;
            //end
            case 2:
            while(childRef != -1)
                {
                    Item child = items[childRef];
                    childPos.x += (LayoutNumber)(child.finalRect.z + child.margin.x + child.margin.z);
                    childPos.y += (LayoutNumber)(child.finalRect.w + child.margin.y + child.margin.w);
                    childRef = child.nextSibling;
                }
                childPos.x = (LayoutNumber)(item.finalRect.z - childPos.x);
                childPos.y = (LayoutNumber)(item.finalRect.w - childPos.y);
                break;
        }
        if(item.flags.StackDirection == 0)
        {
            childPos.y = 0;
        }
        else
        {
            childPos.x = 0;
        }
        childPos.x += pos.x;
        childPos.y += pos.y;
        childRef = item.firstChild;
        while(childRef != -1)
        {
            Item child = items[childRef];
            LayoutVec2 childMarginTotal = new LayoutVec2(
                (LayoutNumber)(child.margin.x + child.margin.z),
                (LayoutNumber)(child.margin.y + child.margin.w)
            );
            LayoutVec2 childRealPos;
            childRealPos = new LayoutVec2(
                (LayoutNumber)(childPos.x + child.margin.x),
                (LayoutNumber)(childPos.y + child.margin.y)
            );
            //set the child's position
            switch(child.flags.PerpendicularAllignment)
            {
                //center
                case 1:
                    if(item.flags.StackDirection == 0)
                    {
                        childRealPos.y = (LayoutNumber)(childPos.y + item.finalRect.w/2 - child.finalRect.w/2);
                    }
                    else
                    {
                        childRealPos.x = (LayoutNumber)(childPos.x + item.finalRect.z/2 - child.finalRect.z/2);
                    }
                    break;
                //end
                case 2:
                    if(item.flags.StackDirection == 0)
                    {
                        childRealPos.y = (LayoutNumber)(childPos.y + item.finalRect.w - child.finalRect.w - child.margin.w);
                    }
                    else
                    {
                        childRealPos.x = (LayoutNumber)(childPos.x + item.finalRect.z - child.finalRect.z - child.margin.z);
                    }
                    break;
            }
            child.finalRect.x = childRealPos.x;
            child.finalRect.y = childRealPos.y;
            items[childRef] = child;
            DeterminePositions(childRef, childRealPos);
            //determine the position of the next one
            if(item.flags.StackDirection == 0)
            {
                //horizontal
                childPos.x += (LayoutNumber)(child.finalRect.z + childMarginTotal.x);
            }
            else
            {
                //vertical
                childPos.y += (LayoutNumber)(child.finalRect.w + childMarginTotal.y);
            }
            childRef = child.nextSibling;
        }

    }
}