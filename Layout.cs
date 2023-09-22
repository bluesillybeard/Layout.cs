namespace Layout;

//inspired by a C header: https://github.com/randrew/layout
// I was originally going to directly port it to C#,
// but it has a lot of strangeness and C stuff that I don't like.
// So, I made my own version.


// This code is not well made.
// TODO: make this not a big mess
// These are configurable to your liking.
using LayoutNumber = Int16;

public struct LayoutVec2
{
    public LayoutVec2(LayoutNumber x, LayoutNumber y)
    {
        this.x = x;
        this.y = y;
    }
    public LayoutNumber x, y;
    //Why an indexer?
    // since the X and Y coordinates are more or less the same,
    // just on different axis,
    // using a number that gets passed into a function
    // to differentiate between the axis
    // is a lot easier than having two different functions
    public LayoutNumber this[uint i]
    {
        //0 -> horizontal
        //1 -> vertical
        readonly get {
            return i==0 ? x : y;
        }
        set
        {
            //Why don't I just use an array?
            // Because C# has no struct-like arrays.
            // And for performance, I want to follow as few references
            // and allocate as few objects as possible.
            // I suppose I could use an explicit packing order
            // and then index from the address of the struct
            // Like one may do in C, but that sounds pretty awful.
            // The layout.h this was inspired from did use arrays, because C is capable of such insanity.
            if(i==0)
            {
                x = value;
            }
            else
            {
                y = value;
            }
        }
    }
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

    public LayoutNumber this[uint i]
    {
        //0=x, 1=y, 2=z, 3=w
        readonly get
        {
            switch(i)
            {
                case 0:
                    return x;
                case 1:
                    return y;
                case 2:
                    return z;
                //technically this means anythig >3 is w, but that shouldn't be a problem.
                default:
                    return w;
            }
        }
        set
        {
            switch(i)
            {
                case 0:
                    x = value; break;
                case 1:
                    y = value;break;
                case 2:
                    z = value;break;
                //technically this means anythig >3 is w, but that shouldn't be a problem.
                default:
                    w = value;break;
            }
        }
    }
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
        readonly get { return (uint)(flags & SDM) >> SDL; }
        set { flags = (uint)(flags & ~SDM | (value << SDL) & SDM); }
    }
    ///<summary>0->start, 1->center, 2->end</summary>
    public uint Allignment {
        readonly get { return (uint)(flags & AM) >> AL; }
        set { flags = (uint)(flags & ~AM | (value << AL) & AM); }
    }
    ///<summary>0->no wrap, 1->wrap</summary>
    public uint Wrap {
        readonly get { return (uint)(flags & WM) >> WL; }
        set { flags = (uint)(flags & ~WM | (value << WL) & WM); }
    }
    ///<summary>0->no fill, 1->fill</summary>
    public uint Fill {
        readonly get { return (uint)(flags & FM) >> FL; }
        set { flags = (uint)(flags & ~FM | (value << FL) & FM); }
    }
    ///<summary>0->no expand, 1->expand</summary>
    public uint Expand {
        readonly get { return (uint)(flags & EM) >> EL; }
        set { flags = (uint)(flags & ~EM | (value << EL) & EM); }
    }
    ///<summary>0->start, 1->center, 2->end</summary>
    public uint PerpendicularAllignment {
        readonly get { return (uint)(flags & PAM) >> PAL; }
        set { flags = (uint)(flags & ~PAM | (value << PAL) & PAM); }
    }
    ///<summary>0->no break, 1->break</summary>
    public uint Break {
        readonly get { return (uint)(flags & BM) >> BL; }
        set { flags = (uint)(flags & ~BM | (value << BL) & BM); }
    }
    ///<summary>0->not in tree, 1->in tree</summary>
    public uint InTree {
        readonly get { return (uint)(flags & NRM) >> NRL; }
        set { flags = (uint)(flags & ~NRM | (value << NRL) & NRM); }
    }

}
public class Item
{
    public Item()
    {
        flags = new ItemFlags();
        parent = null;
        firstChild = null;
        nextSibling = null;
        previousSibling = null;
        minSize = new LayoutVec2();
        maxSize = new LayoutVec2();
        finalRect = new LayoutVec4();
        margin = new LayoutVec4();
    }
    public ItemFlags flags;
    public Item? parent;
    public Item? firstChild;
    public Item? nextSibling;
    public Item? previousSibling;
    public LayoutVec2 minSize;
    public LayoutVec2 maxSize;
    //x=left, y=top, z=width, w=height,
    public LayoutVec4 finalRect;
    //x=left, y=top, z=right, w=bottom
    public LayoutVec4 margin;
}

public class LayoutManager
{
    public Item root;
    public LayoutManager()
    {
        root = InitItemInTree();
    }

    private static Item InitItemInTree()
    {
        Item item = new();
        //The default value for InTree is false.
        item.flags.InTree = 1;
        return item;
    }

    //methods that build the layout
    public Item CreateChild(Item parent)
    {
        Item i = new();
        i.flags.InTree = 1;
        AddChild(parent, i);
        return i;
    }

    public Item CreateChild(Item parent, ItemFlags flags, LayoutVec2 minSize, LayoutVec4 margins)
    {
        Item i = new(){
            flags = flags,
            minSize = minSize,
            margin = margins,
        };
        i.flags.InTree = 1;
        AddChild(parent, i);
        return i;
    }

    public Item CreateSibling(Item item)
    {
        Item i = new();
        i.flags.InTree = 1;
        AddSibling(item, i);
        return i;
    }

    // public void SetItemFlags(Item Item, ItemFlags flags)
    // {
    //     Item item = items[Item];
    //     item.flags = flags;
    //     items[Item] = item;
    // }
    // public void SetItemMinSize(Item Item, LayoutVec2 minSize)
    // {
    //     Item item = items[Item];
    //     item.minSize = minSize;
    //     items[Item] = item;
    // }
    // public void SetItemMaxSize(Item Item, LayoutVec2 maxSize)
    // {
    //     Item item = items[Item];
    //     item.maxSize = maxSize;
    //     items[Item] = item;
    // }
    // public void SetItemMargin(Item Item, LayoutVec4 margin)
    // {
    //     Item item = items[Item];
    //     item.margin = margin;
    //     items[Item] = item;
    // }
    // public void SetItem(Item Item, ItemFlags flags, LayoutVec2 minSize, LayoutVec2 maxSize, LayoutVec4 margin)
    // {
    //     Item item = items[Item];
    //     item.flags = flags;
    //     item.minSize = minSize;
    //     item.maxSize = maxSize;
    //     item.margin = margin;
    //     items[Item] = item;
    // }

    // public ItemFlags GetItemFlags(Item item)
    // {
    //     return items[item].flags;
    // }
    // public Item GetItemParent(Item item)
    // {
    //     return items[item].parent;
    // }

    // public Item GetItemFirstChild(Item item)
    // {
    //     return items[item].firstChild;
    // }
    // public Item GetItemNextSibling(Item item)
    // {
    //     return items[item].nextSibling;
    // }
    // public Item GetItemPreviousSibling(Item item)
    // {
    //     return items[item].previousSibling;
    // }

    // public LayoutVec2 GetItemMinSize(Item item)
    // {
    //     return items[item].minSize;
    // }
    // public LayoutVec2 GetItemMaxSize(Item item)
    // {
    //     return items[item].maxSize;
    // }
    // public LayoutVec4 GetItemFinalRect(Item item)
    // {
    //     return items[item].finalRect;
    // }
    // public LayoutVec4 GetItemMargin(Item item)
    // {
    //     return items[item].margin;
    // }
    //removes an item and its entire subtree.
    public void Remove(Item item)
    {
        if(item == root)
            throw new Exception("Cannot remove the root node!");
        // This recursive function doesn't actually remove anything,
        // it just marks all of the tree as being removed.
        if(item.firstChild != null)
            RemoveInternal(item.firstChild);
        //Unlike the internal remove function,
        // this one has to actually remove references
        // and set the tree back to being valid
        if(item.parent == null)
            throw new Exception("non-root item's parent is null - this SHOULD NOT happen");
        Item parent = item.parent;
        if(parent.firstChild == item)
        {
            // this is the first child
            parent.firstChild = item.nextSibling;
            if(item.nextSibling != null)
            {
                //this does have a sibling
                Item sibling = item.nextSibling;
                sibling.previousSibling = null;
            }
        }
        else
        {
            //we aren't the first child
            if(item.previousSibling == null)
                throw new Exception("non first sibling item's previous item is null - this shouldn't happen!");
            Item previousSibling = item.previousSibling;
            
            previousSibling.nextSibling = item.nextSibling;
            if(item.nextSibling != null)
            {
                // this isn't the last one
                Item nextSibling = item.nextSibling;
                nextSibling.previousSibling = item.previousSibling;
            }
        }
        MarkAsRemoved(item);
    }
    public void Clear()
    {
        root = InitItemInTree();
    }
    void RemoveInternal(Item item)
    {
        //First, remove the child (depth first)
        if(item.firstChild != null)
            RemoveInternal(item.firstChild);
        //We also need to remove this nodes siblings.
        if(item.nextSibling != null)
            RemoveInternal(item.nextSibling);
        MarkAsRemoved(item);
    }

    static void MarkAsRemoved(Item item)
    {
        //if this is the last item, just shrink the list
        item.flags.InTree = 0;
    }

    void AddChild(Item parent, Item newChild)
    {
        if(parent.firstChild == null)
        {
            parent.firstChild = newChild;
            return;
        }
        AddSibling(parent.firstChild, newChild);
    }

    void AddSibling(Item item, Item newSibling)
    {
        Item iteration = item;
        for(;;)
        {
            if(iteration.nextSibling == null)
            {
                iteration.nextSibling = newSibling;
                return;
            }
            iteration = iteration.nextSibling;
        }
    }
    //when the items are iterated:
    // Their sizes are determined first, depth first iteration.
    // Then they are positioned, also depth first iteration.

    //Iterates the entire GUI tree
    // and determines every elements bounds
    public void DetermineLayout(LayoutVec2 windowSize)
    {
        DetermineSizeMinimums(root);
        //DetermineSizes only works on the item's children,
        // So we also need to do the root node
        root.finalRect.z = windowSize.x;
        root.finalRect.w = windowSize.y;
        DetermineSizes(root, windowSize);
        DeterminePositions(root, new LayoutVec2(0, 0));
    }

    //This only figures out the smallest size that the children must be.
    //TODO: I might be able to incorperate this function into the DetermineSizes function,
    // Only do that AFTER they are both complete however
    // because the maxSize and wrap parameters will make the DetermineSizes function a lot more complex
    void DetermineSizeMinimums(Item item)
    {
        //iterate the tree recursively depth first
        if(item.firstChild != null)
            DetermineSizeMinimums(item.firstChild);
        if(item.nextSibling != null)
            DetermineSizeMinimums(item.nextSibling);
        
        LayoutVec2 size = new LayoutVec2();
        //If there are no children, then the minimum size is super simple
        if(item.firstChild == null)
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
                Item? iterator = item.firstChild;
                while(iterator != null)
                {
                    //the item's "min size" is what's defined by the user of the library.
                    // the min size we need to use here is the calculated min size,
                    // which was conveniently written to the finalRect when the children were iterated.
                    // But, we also add the margin to that size, since the container also contains its childrens margins
                    LayoutNumber itemMinSizeX = (LayoutNumber)(iterator.finalRect.z + iterator.margin.x + iterator.margin.z);
                    LayoutNumber itemMinSizeY = (LayoutNumber)(iterator.finalRect.w + iterator.margin.y + iterator.margin.w);
                    size.x += itemMinSizeX;
                    if(itemMinSizeY > size.y)
                    {
                        size.y = itemMinSizeY;
                    }
                    iterator = iterator.nextSibling;
                }
            }
            else
            {
                //vertial stack direction
                // total item sizes in the stacking direction,
                // find the largest element in the perpendicular direction
                Item? iterator = item.firstChild;
                while(iterator != null)
                {
                    //the item's "min size" is what's defined by the user of the library.
                    // the min size we need to use here is the calculated min size,
                    // which was conveniently written to the finalRect when the children were iterated.
                    // But, we also add the margin to that size, since the container also contains its childrens margins
                    LayoutNumber itemMinSizeX = (LayoutNumber)(iterator.finalRect.z + iterator.margin.x + iterator.margin.z);
                    LayoutNumber itemMinSizeY = (LayoutNumber)(iterator.finalRect.w + iterator.margin.y + iterator.margin.w);
                    size.y += itemMinSizeY;
                    if(itemMinSizeX > size.x)
                    {
                        size.x = itemMinSizeX;
                    }
                    iterator = iterator.nextSibling;
                }
            }
        }
        else
        {
            //Wrap is really annoying because we really have NO IDEA how big this is going to be.
            // the size depends on how much space is available, so the parents 'minimum size' is completely pointless.
            // In an attempt to make it vaguely useful, the 'minimum' size is the largest elements.
            Item? iterator = item.firstChild;
            while(iterator != null)
            {
                //the item's "min size" is what's defined by the user of the library.
                // the min size we need to use here is the calculated min size,
                // which was conveniently written to the finalRect when the children were iterated.
                // But, we also add the margin to that size, since the container also contains its childrens margins
                LayoutNumber itemMinSizeX = (LayoutNumber)(iterator.finalRect.z + iterator.margin.x + iterator.margin.z);
                LayoutNumber itemMinSizeY = (LayoutNumber)(iterator.finalRect.w + iterator.margin.y + iterator.margin.w);
                if(itemMinSizeX > size.x)
                {
                    size.x = itemMinSizeX;
                }
                if(itemMinSizeY > size.y)
                {
                    size.y = itemMinSizeY;
                }
                iterator = iterator.nextSibling;
            }
        }
        //set the "final" rect output to be our final size
        // This will be used in the final determineSizes function later.
        item.finalRect = new LayoutVec4(0, 0, size.x, size.y);
    }

    void DetermineSizes(Item item, LayoutVec2 maxSize)
    {
        //Figure out the children's sizes
        // The size depends on a lot of things, which is really annoying
        // However, for now it depends exclusively this fill value and the childs expand.
        // TODO: account for child maxSize, Wrap

        Item? child = item.firstChild;
        // Total our number of children
        int numberOfChildren = 0;
        while(child != null)
        {
            child = child.nextSibling;
            numberOfChildren++;
        }
        child = item.firstChild;
        //For every child
        while(child != null)
        {
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
            DetermineSizes(child, childSize);
            child = child.nextSibling;
        }
    }

    //Note: does not handle the case when allignment is zero.
    LayoutNumber CalculateAllignmentOffset(Item item, uint stackDirection, uint allignment, uint wrap)
    {
        //First, figure out how wide our set of elements is going to be
        LayoutNumber itemSize = item.finalRect[stackDirection+2];
        //summate the sizes of the children in the stack direction
        LayoutNumber sumOfChildSizes = TotalItemSizes(item.firstChild, stackDirection, out var outerMarginTotal);
        if(wrap == 1)
        {
            sumOfChildSizes = LayoutNumber.Min(sumOfChildSizes, (LayoutNumber)(itemSize + outerMarginTotal));
        }

        
        //If we are centering, then we need to do one last thing
        if(allignment == 1)
        {
            itemSize/=2; sumOfChildSizes/=2;
        }
        return (LayoutNumber)(sumOfChildSizes - itemSize);
    }
    //like DetermineSizes, this determines the positions of the children of the item
    // given its position
    void DeterminePositions(Item item, LayoutVec2 pos)
    {
        //The is the second writing of this function.
        //First, use the allignment to figure out where to start placing the children

        //If the allignment is begin, then don't waste time on deteremining an offset since it will just be zero.
        LayoutVec2 placePos = pos;
        var stackDirection = item.flags.StackDirection;
        var allignment = item.flags.Allignment;
        LayoutNumber allignmentOffset = 0;
        if(allignment != 0)
        {
            allignmentOffset = CalculateAllignmentOffset(item, stackDirection, allignment, item.flags.Wrap);
        }
        placePos[stackDirection] += allignmentOffset;
        // Go through every child
        var child = item.firstChild;
        while(child != null)
        {
            // the position of the child is already in placePos.
            // The loop actually determines the position of the next item.
            LayoutVec2 childMarginTotal = new(
                (LayoutNumber)(child.margin.x + child.margin.z),
                (LayoutNumber)(child.margin.y + child.margin.w)
            );
            LayoutVec2 childRealPos;
            childRealPos = new(
                (LayoutNumber)(placePos.x + child.margin.x),
                (LayoutNumber)(placePos.y + child.margin.y)
            );
            //set the child's position
            var XOrYOther = 1 - stackDirection;
            var ZOrWOther = 3 - stackDirection;
            switch(child.flags.PerpendicularAllignment)
            {
                //center
                case 1:
                    childRealPos[XOrYOther] = (LayoutNumber)(placePos[XOrYOther] + item.finalRect[ZOrWOther]/2 - child.finalRect[ZOrWOther]/2);
                    break;
                //end
                case 2:
                    childRealPos[XOrYOther] = (LayoutNumber)(placePos[XOrYOther] + item.finalRect[ZOrWOther] - child.finalRect[ZOrWOther] - child.margin[ZOrWOther]);
                    break;
            }
            child.finalRect.x = childRealPos.x;
            child.finalRect.y = childRealPos.y;
            DeterminePositions(child, childRealPos);
            if(child.nextSibling != null)
            {
                //determine the position of the next one
                placePos[stackDirection] += (LayoutNumber)(child.finalRect[stackDirection+2] + childMarginTotal[stackDirection]);
                var nextItem = child.nextSibling;
                // If it's going to go outside this container and wrap is enabled put it on the next line.
                if(item.flags.Wrap == 1 && placePos[stackDirection] > pos[stackDirection] + item.finalRect[stackDirection + 2] - nextItem.margin[stackDirection] - nextItem.margin[stackDirection+2] - nextItem.finalRect[stackDirection+2])
                {
                    placePos[XOrYOther] += (LayoutNumber)(child.finalRect[ZOrWOther] + childMarginTotal[XOrYOther]);
                    placePos[stackDirection] = (LayoutNumber)(pos[stackDirection] + allignmentOffset);
                }
            }
            child = child.nextSibling;
        }
    }
    private static LayoutNumber TotalItemSizes(Item? item, uint dimention, out LayoutNumber outerMarginTotal)
    {
        if(item == null)
        {
            outerMarginTotal = 0;
            return 0;
        }
        //the begin of the first item's margin
        outerMarginTotal = item.margin[dimention];
        LayoutNumber total = 0;
        while(item != null)
        {
            total += item.finalRect[dimention+2];
            //Don't forget the margins!
            total +=  item.margin[dimention];
            total += item.margin[dimention+2];
            if(item.nextSibling == null)
            {
                //plus the end of the last childs margin
                outerMarginTotal += item.margin[dimention + 2];
            }
            item = item.nextSibling;
        }
        return total;
    }
}