﻿Imports System.IO
Imports System.Net
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports System.Management
Imports System.Security.Cryptography
Imports System.ComponentModel
Imports System.Text.RegularExpressions
Imports System.Drawing.Imaging
Imports System.Data.SQLite
Imports Tesseract


Public Class Relics
    Private drag As Boolean = False
    Private mouseX As Integer
    Private mouseY As Integer
    Private RelicToHide As TreeNode = Nothing
    Private Relic2ToHide As TreeNode = Nothing
    Public Tree1Sorter As New NodeSorter(0)
    Public Tree2Sorter As New NodeSorter(1)
    Private hidden_nodes As JObject                                      ' Contains list of nodes to hide                {"Lith": ["A1","A2",...], "Meso": [...], "Neo": [...], "Axi": [...]}
    Private hidden_file_path As String = Path.Combine(appData, "WFInfo\hidden.json")

    Private Sub pTitle_MouseDown(sender As Object, e As MouseEventArgs) Handles pTitle.MouseDown
        drag = True
        mouseX = Cursor.Position.X - Me.Left
        mouseY = Cursor.Position.Y - Me.Top
    End Sub

    Private Sub pTitle_MouseMove(sender As Object, e As MouseEventArgs) Handles pTitle.MouseMove
        If drag Then
            Me.Top = Cursor.Position.Y - mouseY
            Me.Left = Cursor.Position.X - mouseX
        End If
    End Sub

    Private Sub pTitle_MouseUp(sender As Object, e As MouseEventArgs) Handles pTitle.MouseUp
        drag = False
        My.Settings.RelicWinLoc = Me.Location
    End Sub

    Private Sub lbTitle_MouseDown(sender As Object, e As MouseEventArgs) Handles lbTitle.MouseDown
        drag = True
        mouseX = Cursor.Position.X - Me.Left
        mouseY = Cursor.Position.Y - Me.Top
    End Sub

    Private Sub lbTitle_MouseMove(sender As Object, e As MouseEventArgs) Handles lbTitle.MouseMove
        If drag Then
            Me.Top = Cursor.Position.Y - mouseY
            Me.Left = Cursor.Position.X - mouseX
        End If
    End Sub

    Private Sub lbTitle_MouseUp(sender As Object, e As MouseEventArgs) Handles lbTitle.MouseUp
        drag = False
        My.Settings.RelicWinLoc = Me.Location
    End Sub

    Private Sub btnClose_Click(sender As Object, e As EventArgs) Handles btnClose.Click
        Me.Hide()
    End Sub

    Private Sub RelicTree_Collapse(sender As Object, e As TreeViewEventArgs) Handles RelicTree.AfterCollapse, RelicTree2.AfterCollapse
        Dim temp As String = "|" + e.Node.FullPath.Replace("\", " ") + "|"
        If e.Node.Text <> "Hidden" Then
            temp = temp.Replace("Hidden ", "")
        End If
        If My.Settings.ExpandedRelics.Contains(temp) Then
            My.Settings.ExpandedRelics = My.Settings.ExpandedRelics.Replace(temp, "")
        End If
        Console.WriteLine(My.Settings.ExpandedRelics)
        temp = temp.Replace("|", "")
        Dim era_name As String() = temp.Split(" ")
        If era_name.Count = 2 AndAlso era_name(1).Length = 2 Then
            Console.WriteLine("LOOKING FOR: " + era_name(0) + "---" + era_name(1))
            Dim other As TreeView = RelicTree
            If sender.Equals(RelicTree) Then
                other = RelicTree2
            End If
            For Each node As TreeNode In other.Nodes.Find(era_name(1), True)
                Console.WriteLine(node.FullPath + "---" + node.Text)
                If node.IsExpanded AndAlso node.FullPath.Contains(era_name(0)) Then
                    node.Collapse()
                End If
            Next
        End If
    End Sub

    Private Sub RelicTree_Expand(sender As Object, e As TreeViewEventArgs) Handles RelicTree.AfterExpand, RelicTree2.AfterExpand
        Dim temp As String = "|" + e.Node.FullPath.Replace("\", " ") + "|"
        If e.Node.Text <> "Hidden" Then
            temp = temp.Replace("Hidden ", "")
        End If
        If Not My.Settings.ExpandedRelics.Contains(temp) Then
            My.Settings.ExpandedRelics += temp
        End If
        Console.WriteLine(My.Settings.ExpandedRelics)
        temp = temp.Replace("|", "")
        Dim era_name As String() = temp.Split(" ")
        If era_name.Count = 2 AndAlso era_name(1).Length = 2 Then
            Console.WriteLine("LOOKING FOR: " + era_name(0) + "---" + era_name(1))
            Dim other As TreeView = RelicTree
            If sender.Equals(RelicTree) Then
                other = RelicTree2
            End If
            For Each node As TreeNode In other.Nodes.Find(era_name(1), True)
                Console.WriteLine(node.FullPath + "---" + node.Text)
                If Not node.IsExpanded AndAlso node.FullPath.Contains(era_name(0)) Then
                    node.Expand()
                End If
            Next
        End If
    End Sub

    Private Sub Relics_Opening(sender As Object, e As EventArgs) Handles Me.Shown
        Me.Location = My.Settings.RelicWinLoc
        If My.Settings.TreeOne Then
            RelicTree2.Visible = False
            RelicTree.Select()
            Label2.Text = "Relic Eras"
        Else
            RelicTree.Visible = False
            RelicTree2.Select()
            Label2.Text = "All Relics"
        End If
        SortSelection.SelectedIndex = My.Settings.SortType
    End Sub

    Private Sub RelicTree_DrawItem(ByVal sender As System.Object, ByVal e As System.Windows.Forms.DrawTreeNodeEventArgs) Handles RelicTree.DrawNode, RelicTree2.DrawNode
        e.DrawDefault = True
        If e.Bounds.Width = 0 Then
            Return
        End If
        Dim fullPath As String = e.Node.FullPath.Replace("Hidden\", "")
        If sender.Equals(RelicTree2) Then
            fullPath = ReplaceFirst(fullPath, " ", "|")

        End If
        Dim split As String() = fullPath.Split(New Char() {CChar("\"), CChar("|")})

        ' Write Plat + Ducat values

        Dim sf As New StringFormat With {
            .Alignment = StringAlignment.Far
        }
        If split.Count = 2 Then
            Dim find As JObject = Nothing
            If db.relic_data.TryGetValue(split(0), find) Then
                If find.TryGetValue(split(1), find) Then
                    If find("vaulted") Then
                        Dim right As Integer = 115
                        If fullPath.Contains("|") Then
                            If e.Node.FullPath.Contains("Meso") Then
                                right = 131
                            ElseIf e.Node.FullPath.Contains("Axi") Then
                                right = 118
                            ElseIf e.Node.FullPath.Contains("Lith") Then
                                right = 123
                            Else
                                right = 122
                            End If
                            If e.Node.FullPath.Contains("Hidden") Then
                                right += 20
                            End If

                        ElseIf e.Node.FullPath.Contains("Hidden") Then
                            right = 135
                        End If
                        Dim rect As SizeF = e.Graphics.MeasureString("Vaulted", Me.Font)
                        e.Graphics.FillRectangle(bgBrush, right - rect.Width - 10, e.Bounds.Top + 1, rect.Width + 10, rect.Height)
                        e.Graphics.DrawString("Vaulted", RelicTree.Font, stealthBrush, right, e.Bounds.Top + 1, sf)
                    End If

                    'Double.Parse(ducat_plat(name)("plat"))
                    Dim itot As Double = find("int")
                    Dim rtot As Double = find("rad")

                    Dim rtot_str As String = rtot.ToString("N1")
                    If rtot > 0 Then
                        rtot_str = "+" + rtot_str
                    End If
                    e.Graphics.FillRectangle(bgBrush, 220, e.Bounds.Top, 180, e.Bounds.Height)
                    e.Graphics.DrawString("RAD:", RelicTree.Font, stealthBrush, 255, e.Bounds.Top, sf)
                    e.Graphics.DrawString(rtot_str, RelicTree.Font, stealthBrush, 300, e.Bounds.Top, sf)
                    e.Graphics.DrawImage(My.Resources.plat, 300, e.Bounds.Top + 2, e.Bounds.Height - 4, e.Bounds.Height - 4)
                    e.Graphics.DrawString("INT:", RelicTree.Font, stealthBrush, 350, e.Bounds.Top, sf)
                    e.Graphics.DrawString(itot.ToString("N1"), RelicTree.Font, stealthBrush, 385, e.Bounds.Top, sf)
                    e.Graphics.DrawImage(My.Resources.plat, 385, e.Bounds.Top + 2, e.Bounds.Height - 4, e.Bounds.Height - 4)
                End If
            End If

        ElseIf split.Count = 3 Then
            ' Common - #cd7f32
            ' Uncommon - #c0c0c0
            ' Rare - #ffd700
            Dim brush As Brush = rareBrush
            If e.Node.Index > 2 Then
                brush = commonBrush
            ElseIf e.Node.Index > 0 Then
                brush = uncommonBrush
            End If
            Dim name As String = split(2)
            If name <> "Forma Blueprint" Then
                Dim vals As JObject = db.market_data(name)
                e.Graphics.FillRectangle(bgBrush, 260, e.Bounds.Top, 130, e.Bounds.Height)
                e.Graphics.DrawString(Double.Parse(vals("plat")).ToString("N1"), RelicTree.Font, brush, 300, e.Bounds.Top + 1, sf)
                e.Graphics.DrawImage(My.Resources.plat, 300, e.Bounds.Top + 2, e.Bounds.Height - 4, e.Bounds.Height - 4)
                e.Graphics.DrawString(vals("ducats"), RelicTree.Font, brush, 370, e.Bounds.Top + 1, sf)
                e.Graphics.DrawImage(My.Resources.ducat_w, 370, e.Bounds.Top + 2, e.Bounds.Height - 4, e.Bounds.Height - 4)
            End If
        End If
    End Sub

    Private Sub HideMenu_Click(sender As Object, e As ToolStripItemClickedEventArgs) Handles HideMenu.ItemClicked
        Dim split As String() = RelicToHide.FullPath.Replace("Hidden\", "").Split("\")
        Dim arr As JArray = hidden_nodes(split(0))

        If RelicToHide.FullPath.Contains("Hidden") Then
            Dim parent As TreeNode = RelicToHide.Parent
            parent.Nodes.Remove(RelicToHide)
            parent.Parent.Nodes.Add(RelicToHide)

            Relic2ToHide.Parent.Nodes.Remove(Relic2ToHide)
            RelicTree2.Nodes.Add(Relic2ToHide)
            Dim tok As JToken = arr.SelectToken("$[?(@ == '" + split(1) + "')]")
            arr.Remove(tok)
        Else
            Dim parent As TreeNode = RelicToHide.Parent
            parent.Nodes.Remove(RelicToHide)
            parent.Nodes.Find("Hidden", False)(0).Nodes.Add(RelicToHide)

            RelicTree2.Nodes.Remove(Relic2ToHide)
            RelicTree2.Nodes.Find("Hidden", False)(0).Nodes.Add(Relic2ToHide)
            arr.Add(split(1))
        End If
        File.WriteAllText(hidden_file_path, JsonConvert.SerializeObject(hidden_nodes, Newtonsoft.Json.Formatting.Indented))
    End Sub

    Private Sub RelicTree_Click(sender As Object, e As TreeNodeMouseClickEventArgs) Handles RelicTree.NodeMouseClick, RelicTree2.NodeMouseClick
        If e.Button <> MouseButtons.Right Then
            RelicToHide = Nothing
            Relic2ToHide = Nothing
            Return
        End If
        If sender.Equals(RelicTree) Then
            RelicToHide = e.Node
            Dim era As String = e.Node.FullPath.Split("\")(0)
            For Each node As TreeNode In RelicTree2.Nodes.Find(e.Node.Name, True)
                If node.FullPath.Contains(era) Then
                    Relic2ToHide = node
                    Exit For
                End If
            Next
        Else
            Relic2ToHide = e.Node
            Dim era As String = e.Node.Text.Split(" ")(0)
            For Each node As TreeNode In RelicTree.Nodes.Find(e.Node.Name, True)
                If node.FullPath.Contains(era) Then
                    RelicToHide = node
                    Exit For
                End If
            Next
        End If

        HideMenu.Items.Clear()
        If e.Node IsNot Nothing AndAlso e.Node.Name.Length = 2 Then
            If e.Node.FullPath.Contains("Hidden") Then
                HideMenu.Items.Add("Show").ForeColor = textColor
                HideMenu.Show(RelicTree, e.Location)

            Else
                HideMenu.Items.Add("Hide").ForeColor = textColor
                HideMenu.Show(RelicTree, e.Location)

            End If
        End If
    End Sub

    Private Sub Label2_Click(sender As Object, e As EventArgs) Handles Label2.Click
        RelicTree2.Visible = My.Settings.TreeOne
        My.Settings.TreeOne = Not My.Settings.TreeOne
        RelicTree.Visible = My.Settings.TreeOne
        If My.Settings.TreeOne Then
            RelicTree.Select()
            Label2.Text = "Relic Eras"
        Else
            RelicTree2.Select()
            Label2.Text = "All Relics"
        End If
    End Sub

    Private Sub SortSelection_SelectedIndexChanged(sender As Object, e As EventArgs) Handles SortSelection.SelectedIndexChanged
        My.Settings.SortType = SortSelection.SelectedIndex
        Tree1Sorter.type = My.Settings.SortType
        Tree2Sorter.type = My.Settings.SortType
        RelicTree.Sort()
        RelicTree2.Sort()
        If RelicTree.Visible Then
            RelicTree.Select()
        Else
            RelicTree2.Select()
        End If

    End Sub

    Public Sub Load_Relic_Tree()
        Dim hide As TreeNode = Nothing
        If RelicTree.Nodes(0).Nodes.Count > 1 Then
            Return
        End If
        For Each node As TreeNode In RelicTree.Nodes
            CheckIfExpand(node)

            For Each relic As JProperty In db.relic_data(node.Text)
                Dim kid As New TreeNode(relic.Name)
                kid.Name = relic.Name
                node.Nodes.Add(kid)

                kid.Nodes.Add(relic.Value("rare1").ToString()).ForeColor = rareColor
                kid.Nodes.Add(relic.Value("uncommon1").ToString()).ForeColor = uncommonColor
                kid.Nodes.Add(relic.Value("uncommon2").ToString()).ForeColor = uncommonColor
                kid.Nodes.Add(relic.Value("common1").ToString()).ForeColor = commonColor
                kid.Nodes.Add(relic.Value("common2").ToString()).ForeColor = commonColor
                kid.Nodes.Add(relic.Value("common3").ToString()).ForeColor = commonColor
                Dim rtot As Double = 0
                Dim itot As Double = 0
                Dim rperc As Double = 0.1
                Dim iperc As Double = 0.02
                Dim count As Integer = 0
                For Each temp As TreeNode In kid.Nodes
                    If temp.Parent.FullPath = kid.FullPath Then
                        If db.market_data.TryGetValue(temp.Text, Nothing) Then
                            If count > 2 Then
                                rperc = 0.5 / 3.0
                                iperc = 0.76 / 3.0
                            ElseIf count > 0 Then
                                rperc = 0.2
                                iperc = 0.11
                            End If
                            Dim plat As Double = Double.Parse(db.market_data(temp.Text)("plat"))
                            rtot += (plat * rperc)
                            itot += (plat * iperc)
                            count += 1
                        Else
                            Console.WriteLine("MISSING RELIC PLAT VALUES: " + temp.FullPath + " -- " + temp.Text)
                        End If
                    End If
                Next
                rtot -= itot
                db.relic_data(node.Text)(relic.Name)("rad") = rtot
                db.relic_data(node.Text)(relic.Name)("int") = itot
                CheckIfExpand(kid)

                kid = kid.Clone()
                kid.Text = node.Text + " " + relic.Name
                RelicTree2.Nodes.Add(kid)
                CheckIfExpand(kid)
            Next
            hide = node.Nodes.Add("Hidden")
            hide.Name = "Hidden"
            CheckIfExpand(hide)
        Next
        hide = RelicTree2.Nodes.Add("Hidden")
        hide.Name = "Hidden"
        CheckIfExpand(hide)

        Load_Hidden_Nodes()

        RelicTree.TreeViewNodeSorter = Tree1Sorter
        RelicTree2.TreeViewNodeSorter = Tree2Sorter
        RelicTree.Sort()
        RelicTree2.Sort()
    End Sub

    Private Sub CheckIfExpand(node As TreeNode)
        Dim temp As String = "|" + node.FullPath.Replace("\", " ") + "|"
        If node.Text <> "Hidden" Then
            temp = temp.Replace("Hidden ", "")
        End If
        If My.Settings.ExpandedRelics.Contains(temp) Then
            node.Expand()
        End If
    End Sub

    Private Sub Load_Hidden_Nodes()
        If File.Exists(hidden_file_path) Then
            hidden_nodes = JsonConvert.DeserializeObject(Of JObject)(File.ReadAllText(hidden_file_path))

            For Each node As TreeNode In RelicTree.Nodes
                For Each hide As JValue In hidden_nodes(node.Text)
                    Dim move As TreeNode = node.Nodes.Find(hide.Value, False)(0)
                    node.Nodes.Remove(move)
                    node.Nodes.Find("Hidden", False)(0).Nodes.Add(move)
                    For Each found As TreeNode In RelicTree2.Nodes.Find(hide.Value, False)
                        If found.Text.Equals(node.Text + " " + hide.Value) Then
                            RelicTree2.Nodes.Remove(found)
                            RelicTree2.Nodes.Find("Hidden", False)(0).Nodes.Add(found)
                        End If
                    Next

                Next
            Next
        Else
            hidden_nodes = New JObject()
            hidden_nodes("Lith") = New JArray()
            hidden_nodes("Meso") = New JArray()
            hidden_nodes("Neo") = New JArray()
            hidden_nodes("Axi") = New JArray()
            File.WriteAllText(hidden_file_path, JsonConvert.SerializeObject(hidden_nodes, Newtonsoft.Json.Formatting.Indented))
        End If
    End Sub
End Class

Public Class NodeSorter
    Implements IComparer
    Dim relic As Integer = 0
    Public type As Integer = 0 ' 0: Name, 1: Intact Plat, 2: Rad Bonus
    Dim eras As String() = {"Lith", "Meso", "Neo", "Axi"}

    Public Sub New(x As Integer)
        Me.relic = x
    End Sub

    Private Function IComparer_Compare(x As Object, y As Object) As Integer Implements IComparer.Compare
        Dim tx As TreeNode = x
        Dim ty As TreeNode = y
        Dim strx As String = tx.Text
        Dim stry As String = ty.Text
        Dim erax As String = ""
        Dim eray As String = ""
        If strx.Contains("Prime") OrElse strx.Contains("Forma") OrElse stry.Contains("Prime") OrElse stry.Contains("Forma") Then
            Return tx.Index - ty.Index
        End If

        If Me.relic = 0 Then
            If strx.Length = 4 OrElse strx.Length = 3 Then
                ' Lith < Meso < Neo < Axi
                Dim relics As String() = {"Lith", "Meso", "Neo", "Axi"}
                Return Array.IndexOf(relics, strx) - Array.IndexOf(relics, stry)
            ElseIf strx.Length <> stry.Length Then
                Return strx.Length - stry.Length
            End If
            If tx.Parent Is Nothing OrElse ty.Parent Is Nothing Then
                If Me.type > 0 Then
                    If tx.Parent IsNot Nothing Then
                        erax = tx.Parent.Text
                        If erax = "Hidden" Then
                            erax = tx.Parent.Parent.Text
                        End If
                        eray = erax
                    ElseIf ty.Parent IsNot Nothing Then
                        eray = ty.Parent.Text
                        If eray = "Hidden" Then
                            eray = ty.Parent.Parent.Text
                        End If
                        erax = eray
                    Else
                        Return 0
                    End If
                End If
            Else
                erax = tx.Parent.Text
                If erax = "Hidden" Then
                    erax = tx.Parent.Parent.Text
                End If
                eray = ty.Parent.Text
                If eray = "Hidden" Then
                    eray = ty.Parent.Parent.Text
                End If
            End If
        ElseIf Me.relic = 1 Then
            Dim splitx As String() = strx.Split(" ")
            Dim splity As String() = stry.Split(" ")
            If splitx.Length = 1 Or splity.Length = 1 Then
                ' If one is "Hidden"
                Return splity.Length - splitx.Length
            ElseIf Me.type = 0 AndAlso splitx(0) <> splity(0) Then
                ' If the Eras are different 
                '    AND the sort type is name
                Return Array.IndexOf(eras, splitx(0)) - Array.IndexOf(eras, splity(0))
            End If
            strx = splitx(1)
            stry = splity(1)
            erax = splitx(0)
            eray = splity(0)
        End If
        If Me.type <= 0 Then
            Return String.Compare(strx, stry)
        End If

        Dim jobx As JObject = db.relic_data(erax)(strx)
        Dim joby As JObject = db.relic_data(eray)(stry)
        If Me.type = 1 Then
            Return (Double.Parse(joby("int")) - Double.Parse(jobx("int"))) * 100
        End If
        Return (Double.Parse(joby("rad")) - Double.Parse(jobx("rad"))) * 100
    End Function
End Class