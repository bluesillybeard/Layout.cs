using System.Diagnostics;
using ItemRef = System.Int32;


using Raylib_CsLo;
using System.Runtime.InteropServices;

public static class Program
{
    static readonly LayoutVec4 margin = new LayoutVec4(10, 7, 4, 1);
    public static void Main()
    {
        Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
        Raylib.SetTargetFPS(60);
        Raylib.InitWindow(800, 600, "testing Layout");
        //Step one: build the tree
        Layout l = new Layout();
        BuildUI(l);
        l.DetermineLayout(new LayoutVec2(800, 600));
        while(!Raylib.WindowShouldClose())
        {
            if(Raylib.IsWindowResized())
            {
                l.DetermineLayout(new LayoutVec2((short)Raylib.GetRenderWidth(), (short)Raylib.GetRenderHeight()));
            }
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Raylib.BLACK);
            DrawItemRecursively(0, l);
            Raylib.EndDrawing();
        }
    }

    static void DrawItemRecursively(ItemRef item, Layout l)
    {
        Random r = new Random(item);
        if(item == -1)return;
        LayoutVec4 rect = l.GetItemFinalRect(item);
        //int + int = int. So why is short + short = int? Following that logic, int + int = long. But in C# that is not the case. why. WHY.
        Color color = new Color(r.Next() & 255, r.Next() & 255, r.Next() & 255, 255);
        Raylib.DrawRectangle(rect.x, rect.y, rect.z, rect.w, color);
        DrawItemRecursively(l.GetItemNextSibling(item), l);
        DrawItemRecursively(l.GetItemFirstChild(item), l);
    }
    static void BuildUI(Layout l)
    {
        l.Clear();
        ItemFlags flags = new ItemFlags();
        //minimum size of all of the elements
        LayoutVec2 size = new LayoutVec2(50, 50);
        //the root node
        flags.StackDirection = 1;
        flags.Fill = 1;
        l.SetItemFlags(0, flags);
        l.SetItemMinSize(0, size);
        l.SetItemMargin(0, margin);
        //the first child of the root node
        flags.StackDirection = 1;
        flags.Fill = 0;
        flags.Expand = 1;
        ItemRef firstChildOfTheRootNode = l.CreateChild(0);
        l.SetItemFlags(firstChildOfTheRootNode, flags);
        l.SetItemMinSize(firstChildOfTheRootNode, size);
        l.SetItemMargin(firstChildOfTheRootNode, margin);
        //second child of the root node
        flags.StackDirection = 0;
        flags.Fill = 1;
        flags.Expand = 1;
        ItemRef secondChildOfTheRootNode = l.CreateSibling(firstChildOfTheRootNode);
        l.SetItemFlags(secondChildOfTheRootNode, flags);
        l.SetItemMinSize(secondChildOfTheRootNode, size);
        l.SetItemMargin(secondChildOfTheRootNode, margin);
        //the first child of the first child of the root node
        flags.StackDirection = 0;
        flags.Fill = 0;
        flags.Expand = 1;
        ItemRef firstChildOfTheFirstChildOfTheRootNode = l.CreateChild(firstChildOfTheRootNode);
        l.SetItemFlags(firstChildOfTheFirstChildOfTheRootNode, flags);
        l.SetItemMinSize(firstChildOfTheFirstChildOfTheRootNode, size);
        l.SetItemMargin(firstChildOfTheFirstChildOfTheRootNode, margin);
        //second child of the first child of the root node
        ItemRef secondChildOfTheFirstChildOfTheRootNode = l.CreateChild(firstChildOfTheRootNode);
        l.SetItemFlags(secondChildOfTheFirstChildOfTheRootNode, flags);
        l.SetItemMinSize(secondChildOfTheFirstChildOfTheRootNode, size);
        l.SetItemMargin(secondChildOfTheFirstChildOfTheRootNode, margin);
        // ok these names are getting rediculous, so I made a better notation
        //first child of the second child of the root node
        ItemRef child_2_1 = l.CreateChild(secondChildOfTheRootNode);
        l.SetItemFlags(child_2_1, flags);
        l.SetItemMinSize(child_2_1, size);
        l.SetItemMargin(child_2_1, margin);
        //second child of the second child of the root node
        ItemRef child_2_2 = l.CreateSibling(child_2_1);
        l.SetItemFlags(child_2_2, flags);
        l.SetItemMinSize(child_2_2, size);
        l.SetItemMargin(child_2_2, margin);
        //first child of the second child of the second child of the root node
        flags.Fill = 0;
        flags.Expand = 0;
        ItemRef child_2_2_1 = l.CreateChild(child_2_2);
        l.SetItemFlags(child_2_2_1, flags);
        l.SetItemMinSize(child_2_2_1, size);
        l.SetItemMargin(child_2_2_1, margin);
        flags.Expand = 1;
        ItemRef child_2_2_2 = l.CreateChild(child_2_2);
        l.SetItemFlags(child_2_2_2, flags);
        l.SetItemMinSize(child_2_2_2, size);
        l.SetItemMargin(child_2_2_2, margin);
    }
}