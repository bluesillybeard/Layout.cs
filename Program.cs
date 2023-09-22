using Raylib_CsLo;
using Layout;
public static class Program
{
    static readonly LayoutVec4 margin = new LayoutVec4(10, 7, 4, 1);
    static readonly Action<LayoutManager>[] uiBuilders = new[]
    {
        BuildUI1,
        BuildUI2,

    };
    public static void Main()
    {
        //Not sure if this is a problem with GNOME (my Desktop Environment),
        // X11 (Windowing system), or Raylib, but using VSync makes resizing the window extremely laggy
        Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
        Raylib.SetTargetFPS(60);
        Raylib.InitWindow(800, 600, "testing Layout");
        //Step one: build the tree
        var builder = 0;
        LayoutManager l = new();
        uiBuilders[builder](l);
        l.DetermineLayout(new LayoutVec2(800, 600));
        while(!Raylib.WindowShouldClose())
        {
            bool uiChanged = false;
            if(Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
            {
                builder += 1;
                builder %= uiBuilders.Length;
                uiBuilders[builder](l);
                uiChanged = true;
            }
            if(Raylib.IsWindowResized())
            {
                uiChanged = true;
            }
            if(uiChanged)
            {
                l.DetermineLayout(new LayoutVec2((short)Raylib.GetRenderWidth(), (short)Raylib.GetRenderHeight()));
            }
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Raylib.WHITE);
            int seed = 0;
            DrawItemRecursively(l.root, l, ref seed);
            Raylib.EndDrawing();
        }
    }

    static void DrawItemRecursively(Item? item, LayoutManager l, ref int seed)
    {
        Random r = new(seed++);
        if(item == null)return;
        LayoutVec4 rect = item.finalRect;
        Color color = new(1 - r.Next() & 255, 1 - r.Next() & 255, 1 - r.Next() & 255, 255);

        Raylib.DrawRectangle(rect.x, rect.y, rect.z, rect.w, color);
        DrawItemRecursively(item.nextSibling, l, ref seed);
        DrawItemRecursively(item.firstChild, l, ref seed);
    }
    static void BuildUI1(LayoutManager l)
    {
        Raylib.SetWindowTitle("Testing Layout - random nonsense");
        l.Clear();
        ItemFlags flags = new();
        // a size
        LayoutVec2 size = new(50, 50);
        LayoutVec2 smallSize = new(20, 20);
        //the root node
        flags.StackDirection = 1;
        flags.Fill = 1;
        flags.Allignment = 1;
        l.root.flags = flags;
        l.root.margin = margin;
        //the first child of the root node
        flags.StackDirection = 1;
        flags.Fill = 0;
        flags.Expand = 1;
        Item firstChildOfTheRootNode = l.CreateChild(l.root);
        firstChildOfTheRootNode.flags = flags;
        firstChildOfTheRootNode.minSize = size;
        firstChildOfTheRootNode.margin = margin;
        //second child of the root node
        flags.StackDirection = 0;
        flags.Fill = 1;
        flags.Expand = 1;
        
        Item secondChildOfTheRootNode = l.CreateSibling(firstChildOfTheRootNode);
        secondChildOfTheRootNode.flags = flags;
        secondChildOfTheRootNode.minSize = size;
        secondChildOfTheRootNode.margin = margin;
        //the first child of the first child of the root node
        flags.StackDirection = 0;
        flags.Fill = 0;
        flags.Expand = 1;
        
        Item firstChildOfTheFirstChildOfTheRootNode = l.CreateChild(firstChildOfTheRootNode);
        firstChildOfTheFirstChildOfTheRootNode.flags = flags;
        firstChildOfTheFirstChildOfTheRootNode.minSize = size;
        firstChildOfTheFirstChildOfTheRootNode.margin = margin;
        //second child of the first child of the root node
        flags.Expand = 0;
        
        Item secondChildOfTheFirstChildOfTheRootNode = l.CreateChild(firstChildOfTheRootNode);
        secondChildOfTheFirstChildOfTheRootNode.flags = flags;
        secondChildOfTheFirstChildOfTheRootNode.minSize = size;
        secondChildOfTheFirstChildOfTheRootNode.margin = margin;
        // ok these names are getting rediculous, so I made a better notation
        //first child of the second child of the root node
        flags.Expand = 1;
        flags.Allignment = 0;
        flags.Wrap = 1;
        flags.Fill = 0;
        Item child_2_1 = l.CreateChild(secondChildOfTheRootNode);
        child_2_1.flags = flags;
        child_2_1.minSize = size;
        child_2_1.margin = margin;
        // a bunch of items to test wrapping
        {
            flags.Expand = 0;
            l.CreateChild(child_2_1, flags, smallSize, margin);
            l.CreateChild(child_2_1, flags, smallSize, margin);
            l.CreateChild(child_2_1, flags, smallSize, margin);
            l.CreateChild(child_2_1, flags, smallSize, margin);
            l.CreateChild(child_2_1, flags, smallSize, margin);
            l.CreateChild(child_2_1, flags, smallSize, margin);
            l.CreateChild(child_2_1, flags, smallSize, margin);
            l.CreateChild(child_2_1, flags, smallSize, margin);
            flags.Expand = 1;
        }
        flags.Wrap = 0;
        //second child of the second child of the root node
        Item child_2_2 = l.CreateSibling(child_2_1);
        child_2_2.flags = flags;
        child_2_2.minSize = size;
        child_2_2.margin = margin;
        //first child of the second child of the second child of the root node
        flags.Fill = 0;
        flags.Expand = 0;
        Item child_2_2_1 = l.CreateChild(child_2_2);
        child_2_2_1.flags = flags;
        child_2_2_1.minSize = size;
        child_2_2_1.margin = margin;
        flags.Expand = 1;
        Item child_2_2_2 = l.CreateChild(child_2_2);
        child_2_2_2.flags = flags;
        child_2_2_2.minSize = size;
        child_2_2_2.margin = margin;
    }

    //This is a UI for testing stacking.
    static void BuildUI2(LayoutManager l)
    {
        Raylib.SetWindowTitle("Testing Layout - Wrapping containers");
        l.Clear();
        //the tree looks a bit like this
        /*
        // root
        vertical expand fill{
            horizontal expand fill{
                horizontal expand begin wrap{elements}
                horizontal expand center wrap{elements}
                horizontal expand end wrap{elements}
            }
            horizontal expand fill{
                vertical expand begin wrap{elements}
                vertical expand center wrap{elements}
                vertical expand end wrap{elements}
            }
        }
        */
        LayoutVec2 size = new(20, 20);
        //weird local function
        void AddElements(Item parent, LayoutManager l)
        {
            ItemFlags flags = new()
            {
                Allignment = 0,
                Expand = 0,
                Fill = 0,
                PerpendicularAllignment = 0
            };
            
            l.CreateChild(parent, flags, size, margin);
            l.CreateChild(parent, flags, size, margin);
            l.CreateChild(parent, flags, size, margin);
            l.CreateChild(parent, flags, size, margin);
            l.CreateChild(parent, flags, size, margin);
            l.CreateChild(parent, flags, size, margin);
            l.CreateChild(parent, flags, size, margin);
            l.CreateChild(parent, flags, size, margin);
            l.CreateChild(parent, flags, size, margin);
            l.CreateChild(parent, flags, size, margin);
        }
        //The root element is pretty simple
        l.root.flags = new()
        {
            StackDirection = 1,
            Expand = 1,
            Fill = 1,
        };
        var horizontalGroup = l.CreateChild(l.root, new(){
            Expand = 1,
            Fill = 1,
        }, size, margin);
        var horizontalBegin = l.CreateChild(horizontalGroup, new(){
            Expand = 1,
            Wrap = 1,
        }, size, margin);
        AddElements(horizontalBegin, l);
        var horizontalCenter = l.CreateChild(horizontalGroup, new(){
            Expand = 1,
            Allignment = 1,
            Wrap = 1,
        }, size, margin);
        AddElements(horizontalCenter, l);
        var horizontalEnd = l.CreateChild(horizontalGroup, new(){
            Expand = 1,
            Allignment = 2,
            Wrap = 1,
        }, size, margin);
        AddElements(horizontalEnd, l);

        var verticalGroup = l.CreateChild(l.root, new(){
            Expand = 1,
            Fill = 1,
        }, size, margin);
        var verticalBegin = l.CreateChild(verticalGroup, new(){
            Expand = 1,
            StackDirection = 1,
            Wrap = 1,
        }, size, margin);
        AddElements(verticalBegin, l);
        var verticalCenter = l.CreateChild(verticalGroup, new(){
            Expand = 1,
            Allignment = 1,
            StackDirection = 1,
            Wrap = 1,
        }, size, margin);
        AddElements(verticalCenter, l);
        var verticalEnd = l.CreateChild(verticalGroup, new(){
            Expand = 1,
            Allignment = 2,
            StackDirection = 1,
            Wrap = 1,
        }, size, margin);
        AddElements(verticalEnd, l);
    }
}