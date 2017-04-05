using Sitecore;
using Sitecore.Collections;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Links;
using Sitecore.Resources;
using Sitecore.Shell;
using Sitecore.Shell.Applications.ContentManager.Galleries;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI;

namespace Sitecore.Support.Shell.Applications.ContentManager.Galleries.Links
{
  public class GalleryLinksForm : GalleryForm
  {
    // Fields
    protected Border Links;

    // Methods
    private string GetLinkTooltip(Item reference, ItemLink link)
    {
      string name;
      string str2;
      string str3 = string.Empty;
      if (link.SourceFieldID == ItemIDs.Null)
      {
        name = Translate.Text("Quick Info");
        str2 = string.Format(Translate.Text("The reference from '{0}' section."), name);
      }
      else
      {
        Item ownerItem = Factory.GetDatabase(link.SourceDatabaseName).GetItem(link.SourceItemID, link.SourceItemLanguage, link.SourceItemVersion);
        Field field = new Field(link.SourceFieldID, ownerItem);
        string str4 = field.Item.Version.ToString();
        string str5 = field.Item.Language.ToString();
        name = field.Name;
        str2 = string.Format(Translate.Text("The reference from '{0}' field."), name);
        if (field.Unversioned)
        {
          str3 = Translate.Text("Language:") + " " + str5;
        }
        else if (!field.Shared && !field.Unversioned)
        {
          str3 = string.Format(Translate.Text("Language: {0}, Version: {1}"), str5, str4);
        }
      }
      return string.Format("title=\"{0} {1}\"", str2, str3);
    }

    protected virtual ItemLink[] GetReferences(Item item)
    {
      Assert.ArgumentNotNull(item, "item");
      return item.Links.GetAllLinks(true, true);
    }

    protected virtual ItemLink[] GetRefererers(Item item)
    {
      Assert.ArgumentNotNull(item, "item");
      LinkDatabase linkDatabase = Globals.LinkDatabase;
      Assert.IsNotNull(linkDatabase, "Link database cannot be null");
      return linkDatabase.GetItemReferrers(item, true);
    }

    public override void HandleMessage(Message message)
    {
      Assert.ArgumentNotNull(message, "message");
      base.Invoke(message, true);
      message.CancelBubble = true;
      message.CancelDispatch = true;
    }

    private bool IsHidden(Item item)
    {
      while (item != null)
      {
        if (item.Appearance.Hidden)
        {
          return true;
        }
        item = item.Parent;
      }
      return false;
    }

    protected override void OnLoad(EventArgs e)
    {
      Assert.ArgumentNotNull(e, "e");
      base.OnLoad(e);
      if (!Context.ClientPage.IsEvent)
      {
        StringBuilder result = new StringBuilder();
        Item itemFromQueryString = UIUtil.GetItemFromQueryString(Context.ContentDatabase);
        if (itemFromQueryString != null)
        {
          List<Pair<Item, ItemLink>> referrers = new List<Pair<Item, ItemLink>>();
          foreach (ItemLink link in this.GetRefererers(itemFromQueryString))
          {
            Database database = Factory.GetDatabase(link.SourceDatabaseName, false);
            if (database != null)
            {
              Item item = database.Items[link.SourceItemID];
              if (((item == null) || !this.IsHidden(item)) || UserOptions.View.ShowHiddenItems)
              {
                referrers.Add(new Pair<Item, ItemLink>(item, link));
              }
            }
          }
          if (referrers.Count > 0)
          {
            this.RenderReferrers(result, referrers);
          }
          referrers = new List<Pair<Item, ItemLink>>();
          foreach (ItemLink link2 in this.GetReferences(itemFromQueryString))
          {
            Database database2 = Factory.GetDatabase(link2.TargetDatabaseName, false);
            if (database2 != null)
            {
              Item item3 = database2.Items[link2.TargetItemID];
              if (((item3 == null) || !this.IsHidden(item3)) || UserOptions.View.ShowHiddenItems)
              {
                referrers.Add(new Pair<Item, ItemLink>(item3, link2));
              }
            }
          }
          if (referrers.Count > 0)
          {
            this.RenderReferences(result, referrers);
          }
        }
        if (result.Length == 0)
        {
          result.Append(Translate.Text("This item has no references."));
        }
        this.Links.Controls.Add(new LiteralControl(result.ToString()));
      }
    }

    private void RenderReferences(StringBuilder result, List<Pair<Item, ItemLink>> references)
    {
      result.Append("<div style=\"font-weight:bold;padding:2px 0px 4px 0px\">" + Translate.Text("References:") + "</div>");
      foreach (Pair<Item, ItemLink> local1 in references)
      {
        Item reference = local1.Part1;
        ItemLink link = local1.Part2;
        if (reference == null)
        {
          result.Append(string.Format("<div class=\"scLink\">{0} {1}: {2}, {3}</div>", new object[] { Images.GetImage("Applications/16x16/error.png", 16, 16, "absmiddle", "0px 4px 0px 0px"), Translate.Text("Not found"), link.TargetDatabaseName, link.TargetItemID }));
        }
        else
        {
          string linkTooltip = this.GetLinkTooltip(reference, link);
          result.Append(string.Concat(new object[] { "<a href=\"#\" class=\"scLink\" ", linkTooltip, " onclick='javascript:return scForm.invoke(\"item:load(id=", reference.ID, ",language=", reference.Language, ",version=", reference.Version, ")\")'>", Images.GetImage(reference.Appearance.Icon, 16, 16, "absmiddle", "0px 4px 0px 0px"), reference.DisplayName, " - [", reference.Paths.Path, "]</a>" }));
        }
      }
    }

    private void RenderReferrers(StringBuilder result, List<Pair<Item, ItemLink>> referrers)
    {
      result.Append("<div style=\"font-weight:bold;padding:2px 0px 4px 0px\">" + Translate.Text("Referrers:") + "</div>");
      foreach (Pair<Item, ItemLink> local1 in referrers)
      {
        Item item = local1.Part1;
        ItemLink link = local1.Part2;
        Item sourceItem = null;
        if (link != null)
        {
          sourceItem = link.GetSourceItem();
        }
        if (item == null)
        {
          result.Append(string.Format("<div class=\"scLink\">{0} {1}: {2}, {3}</div>", new object[] { Images.GetImage("Applications/16x16/error.png", 16, 16, "absmiddle", "0px 4px 0px 0px"), Translate.Text("Not found"), link.SourceDatabaseName, link.SourceItemID }));
        }
        else
        {
          string str = item.Language.ToString();
          string str2 = item.Version.ToString();
          if (sourceItem != null)
          {
            str = sourceItem.Language.ToString();
            str2 = sourceItem.Version.ToString();
          }
          result.Append(string.Concat(new object[] { "<a href=\"#\" class=\"scLink\" onclick='javascript:return scForm.invoke(\"item:load(id=", item.ID, ",language=", str, ",version=", str2, ")\")'>", Images.GetImage(item.Appearance.Icon, 16, 16, "absmiddle", "0px 4px 0px 0px"), item.DisplayName }));
          if ((link != null) && !link.SourceFieldID.IsNull)
          {
            Field field = item.Fields[link.SourceFieldID];
            if (!string.IsNullOrEmpty(field.DisplayName))
            {
              result.Append(" - ");
              result.Append(field.DisplayName);
              if (sourceItem != null)
              {
                Field field2 = sourceItem.Fields[link.SourceFieldID];
                if ((field2 != null) && !field2.HasValue)
                {
                  result.Append(" <span style=\"color:#999999\">");
                  result.Append(Translate.Text("[inherited]"));
                  result.Append("</span>");
                }
              }
            }
          }
          result.Append(" - [" + item.Paths.Path + "]</a>");
        }
      }
    }
  }
}
