using UnityEngine;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine.UI;
using MaterialUI;
using Newtonsoft.Json;
using System.IO;

public class ParseController : MonoBehaviour
{   
    [Header("Visual")]
    public GameObject treasurePrefab;
    public GameObject treasuresHolderObject;
    public GameObject setPrefab;
    public GameObject setsHolderObject;
    public GameObject itemPrefab;
    public GameObject itemsHolderObject;
    public LinearProgressIndicator linearProgressIndicator;
    public Text progressBarValueText;
    public Toggle parseSetsContentToggle;
    public Toggle parseTreasuresContentToggle;
    private int curProgressBarValue = 0;
    private int maxProgressBarValue = 0;

    [Header("Lists")]
    public List<Item> items;
    public List<TreasureView> treasuresViews;
    public List<SetView> setsViews;
    public List<ItemView> itemsViews;
    private PoolingSystem poolingSystem;
    private List<string> treasuresLinks;

    [Header("Parser info")]
    public string wikiSiteURL = "http://dota2.gamepedia.com";
    public string treasuresPageUrl = "http://dota2.gamepedia.com/Treasure";
    public string itemsFileName = "items.json";
    private static HtmlAgilityPack.HtmlDocument _doc;
    private static HtmlWeb _webGet;


    void Start()
    {
        // Parse();
    }

    public void Parse()
    {
        Init();

        GetAllTreasuresUrls();

        StartCoroutine(ParseAllTreasures());

        // StartCoroutine(ParseAllUndefinedItems());

        // DrawTables();
    }

    private void Init()
    {
        treasuresLinks = new List<string>();
        items = new List<Item>();
        treasuresViews = new List<TreasureView>();
        poolingSystem = PoolingSystem.Instance;

        curProgressBarValue = 0;
        maxProgressBarValue = 0;
        progressBarValueText.text = curProgressBarValue + " " + maxProgressBarValue;
        linearProgressIndicator.SetProgress(0);
    }

    void GetAllTreasuresUrls()
    {
        _webGet = new HtmlWeb();
        _doc = _webGet.Load(treasuresPageUrl);

        HtmlNode _treasuresTable = _doc.DocumentNode.SelectSingleNode("//table[@class='navbox']");

        string curTreasureURL = "";
        string lastTreasureURL = "";

        foreach (HtmlNode link in _treasuresTable.ChildNodes.Descendants("a"))
        {
            HtmlAttribute att = link.Attributes["href"];
            curTreasureURL = att.Value;

            if (lastTreasureURL != curTreasureURL 
                /*&& !curTreasureURL.Contains("Bonus")*/ 
                && !att.Value.Contains("Template")
                /*&& att.Value.Contains("_-_")*/
                && !att.Value.Contains("/Ancient_Scroll_Case")
                && !att.Value.Contains("/Locked_Gemstone_Cache")
                && !att.Value.Contains("/Portfolio_of_Heroes_Triumphant")
                && !att.Value.Contains("/The_International_2015_Effigy_Reforger_Pack"))
            {
                treasuresLinks.Add(att.Value);;

                lastTreasureURL = att.Value;            
            }
        }

        treasuresLinks.Reverse();

        foreach(string url in treasuresLinks)
        {
            items.Add(new Item(Item.Class.Treasure));
            items[Item.Count-1].Url = url;
        }

        curProgressBarValue = 0;
        maxProgressBarValue = items.Count;
        progressBarValueText.text = curProgressBarValue + " " + maxProgressBarValue;
    }

    IEnumerator ParseAllTreasures()
    {
        foreach(Item treasure in items.Where( item => item.Type == Item.Class.Treasure )
            .OrderBy( item => item.Id )
            .ToList())
        {
            ParseTreasure(treasure);
            
            yield return null;
        }

        DrawTables();

        if (parseTreasuresContentToggle.isOn)
            ParseAllUndefinedItems();
    }

    void ParseTreasure(Item treasure)
    {
        _webGet = new HtmlWeb();
        _doc = _webGet.Load(wikiSiteURL + treasure.Url);

        treasure.Name = _doc.DocumentNode.SelectSingleNode("//*[@id=\"firstHeading\"]").InnerText;
        treasure.Rare = _doc.DocumentNode.SelectSingleNode("//tr[4]/td/div/a/span/b").InnerText;
        treasure.ImageURL = _doc.DocumentNode.SelectSingleNode("//tr[2]/td/a/img").Attributes["src"].Value;

        StartCoroutine(LoadImage(treasure));
       
        // _treasures[num].imgDiv2 = ResizeImage(_treasures[num].img, 128, 85);

        HtmlNodeCollection h2 = _doc.DocumentNode.SelectNodes("//h2//span[@id]");
        HtmlNodeCollection h3 = _doc.DocumentNode.SelectNodes("//h3//span[@id]");
        HtmlNodeCollection h4 = _doc.DocumentNode.SelectNodes("//h4//span[@id]");

        HtmlNodeCollection allContent = new HtmlNodeCollection(null);

        if (h2 != null)
            for (int i = 0; i < h2.Count; i++)
            {
                allContent.Add(h2[i]);
            }

        if (h3 != null)
            for (int i = 0; i < h3.Count; i++)
            {
                allContent.Add(h3[i]);
            }

        if (h4 != null)
            for (int i = 0; i < h4.Count; i++)
            {
                allContent.Add(h4[i]);
            }

        /*Sorting allContent*/
        for (int i = 0; i < allContent.Count; i++)
        {
            for (int j = i + 1; j < allContent.Count; j++)
            {
                if (allContent[j].Line < allContent[i].Line)
                {
                    var temp = allContent[i];
                    allContent[i] = allContent[j];
                    allContent[j] = temp;
                }
            }
        }

        HtmlNode compendiumTable = _doc.DocumentNode.SelectSingleNode("//table[@class='navbox']");
        if (compendiumTable == null) compendiumTable = allContent[allContent.Count-1];
        // else print("Has navbox line: " + compendiumTable.Line + " " + treasure.Id + " " + treasure.Name);

        HtmlNodeCollection allLinks = _doc.DocumentNode.SelectNodes("//div/div/a");

        string lastSetOrItemURL = "";

        for (int i = 0; i < allContent.Count; i++)
        {
            if (allContent[i].InnerText == "Regular"
                || allContent[i].InnerText == "Equipment"
                || allContent[i].InnerText == "Sets"
                || allContent[i].InnerText == "Bronze Tier"
                || allContent[i].InnerText == "Silver Tier"
                || allContent[i].InnerText == "Gold Tier"
                || allContent[i].InnerText == "Contents"
                || allContent[i].InnerText == "Common")
            {
                foreach (var link in allLinks)
                {
                    if (link.Line > allContent[i].Line
                        && link.Line < allContent[i + 1].Line
                        && link.Line < compendiumTable.Line
                        && link.Attributes["href"].Value != lastSetOrItemURL
                        && !link.Attributes["href"].Value.Contains("?version=")
                        && !link.Attributes["href"].Value.Contains("index.php")
                        && !link.Attributes["href"].Value.Contains("Compendium")
                        && !link.Attributes["href"].Value.Contains("Random")
                        && !link.Attributes["href"].Value.Contains("Upgrade")
                        && !link.Attributes["href"].Value.Contains("Gem")
                        && !link.Attributes["href"].Value.Contains("_Pass"))
                    {
                        if(treasure.Contains == null) treasure.Contains = new List<Item>();
                        
                        Item existItem = CheckExist(link.Attributes["href"].Value);
                        if (existItem == null)
                        {
                            Item newUndefinedItem = new Item(
                                Item.Class.Undefined, 
                                link.Attributes["href"].Value,
                                Item.Chance.Regular,
                                treasure
                            );

                            treasure.Contains.Add(newUndefinedItem);
                            items.Add(newUndefinedItem);
                        }
                        else
                        {
                            treasure.Contains.Add(existItem);
                            existItem.DropChance = Item.Chance.Regular;
                        }

                        lastSetOrItemURL = link.Attributes["href"].Value;
                    }
                }
            }


            // Rare
            // if (//allContent[i].InnerText == "Very Rare"
            //     /*||*/ allContent[i].InnerText == "Rare"
            //     //|| allContent[i].InnerText == "Very Rare Bonus"
            //     || allContent[i].InnerText == "Rare Bonus")
            // {
            //     foreach (var link in allLinks)
            //     {
            //         if (link.Line > allContent[i].Line
            //             && link.Line < allContent[i + 1].Line
            //             && link.Line < compendiumTable.Line
            //             && link.Attributes["href"].Value != lastSetOrItemURL
            //             && !link.Attributes["href"].Value.Contains("?version=")
            //             && !link.Attributes["href"].Value.Contains("index.php")
            //             && !link.Attributes["href"].Value.Contains("Compendium")
            //             && !link.Attributes["href"].Value.Contains("Random")
            //             && !link.Attributes["href"].Value.Contains("Upgrade")
            //             && !link.Attributes["href"].Value.Contains("Gem")
            //             && !link.Attributes["href"].Value.Contains("_Pass"))
            //         {
            //             // if (_treasures[num].veryRareSetOrItem == null)
            //             //     _treasures[num].veryRareSetOrItem = new ArrayList();

            //             // Console.WriteLine("\v" + link.Attributes["href"].Value + " " + link.Line);
            //             // _treasures[num].veryRareSetOrItem.Add(link.Attributes["href"].Value);
            //             // //                            Console.WriteLine(_treasures[num].regularSetOrItem[_treasures[num].regularSetOrItem.Count - 1]);

            //             // lastSetOrItemURL = link.Attributes["href"].Value;
            //             if(treasure.Contains == null) treasure.Contains = new List<Item>();
                        
            //             Item existItem = CheckExist(link.Attributes["href"].Value);
            //             if (existItem == null)
            //             {
            //                 treasure.Contains.Add(
            //                     new Item(
            //                         Item.Class.Undefined, 
            //                         link.Attributes["href"].Value,
            //                         Item.Chance.Rare,
            //                         treasure
            //                         )
            //                 );
            //             }
            //             else
            //             {
            //                 treasure.Contains.Add(existItem);
            //                 existItem.DropChance = Item.Chance.Rare;
            //             }

            //             lastSetOrItemURL = link.Attributes["href"].Value;
            //         }
            //     }
            // }

            if (allContent[i].InnerText == "Very Rare"
                || allContent[i].InnerText == "Rare"
                || allContent[i].InnerText == "Very Rare Bonus"
                || allContent[i].InnerText == "Rare Bonus")
            {
                foreach (var link in allLinks)
                {
                    if (link.Line > allContent[i].Line
                        && link.Line < allContent[i + 1].Line
                        && link.Line < compendiumTable.Line
                        && link.Attributes["href"].Value != lastSetOrItemURL
                        && !link.Attributes["href"].Value.Contains("?version=")
                        && !link.Attributes["href"].Value.Contains("index.php")
                        && !link.Attributes["href"].Value.Contains("Compendium")
                        && !link.Attributes["href"].Value.Contains("Random")
                        && !link.Attributes["href"].Value.Contains("Upgrade")
                        && !link.Attributes["href"].Value.Contains("Gem")
                        && !link.Attributes["href"].Value.Contains("_Pass"))
                    {
                        if(treasure.Contains == null) treasure.Contains = new List<Item>();
                        
                        Item existItem = CheckExist(link.Attributes["href"].Value);
                        if (existItem == null)
                        {
                            Item newUndefinedItem = new Item(
                                Item.Class.Undefined, 
                                link.Attributes["href"].Value,
                                Item.Chance.VeryRare,
                                treasure
                            );

                            treasure.Contains.Add(newUndefinedItem);
                            items.Add(newUndefinedItem);
                        }
                        else
                        {
                            treasure.Contains.Add(existItem);
                            existItem.DropChance = Item.Chance.VeryRare;
                        }

                        lastSetOrItemURL = link.Attributes["href"].Value;
                    }
                }
            }

            if (allContent[i].InnerText == "Extremely Rare"
                || allContent[i].InnerText == "Extremely Rare Bonus"
                || allContent[i].InnerText == "Extremely Bonus"
                || allContent[i].InnerText == "Every Fifth Treasure")
            {
                foreach (var link in allLinks)
                {
                    if (link.Line > allContent[i].Line
                        && link.Line < allContent[i + 1].Line
                        && link.Line < compendiumTable.Line
                        && link.Attributes["href"].Value != lastSetOrItemURL
                        && !link.Attributes["href"].Value.Contains("?version=")
                        && !link.Attributes["href"].Value.Contains("index.php")
                        && !link.Attributes["href"].Value.Contains("Compendium")
                        && !link.Attributes["href"].Value.Contains("Random")
                        && !link.Attributes["href"].Value.Contains("Upgrade")
                        && !link.Attributes["href"].Value.Contains("Gem")
                        && !link.Attributes["href"].Value.Contains("_Pass"))
                    {
                        if(treasure.Contains == null) treasure.Contains = new List<Item>();
                        
                        Item existItem = CheckExist(link.Attributes["href"].Value);
                        if (existItem == null)
                        {
                            Item newUndefinedItem = new Item(
                                Item.Class.Undefined, 
                                link.Attributes["href"].Value,
                                Item.Chance.ExtremelyRare,
                                treasure
                            );

                            treasure.Contains.Add(newUndefinedItem);
                            items.Add(newUndefinedItem);
                        }
                        else
                        {
                            treasure.Contains.Add(existItem);
                            existItem.DropChance = Item.Chance.ExtremelyRare;
                        }

                        lastSetOrItemURL = link.Attributes["href"].Value;
                    }
                }
            }

            if (allContent[i].InnerText == "Ultra Rare"
                || allContent[i].InnerText == "Couriers"
                || allContent[i].InnerText == "Super Very Rare Bonus")
            {
                foreach (var link in allLinks)
                {
                    if (link.Line > allContent[i].Line
                        && link.Line < allContent[i + 1].Line
                        && link.Line < compendiumTable.Line
                        && link.Attributes["href"].Value != lastSetOrItemURL
                        && !link.Attributes["href"].Value.Contains("?version=")
                        && !link.Attributes["href"].Value.Contains("index.php")
                        && !link.Attributes["href"].Value.Contains("Compendium")
                        && !link.Attributes["href"].Value.Contains("Random")
                        && !link.Attributes["href"].Value.Contains("Upgrade")
                        && !link.Attributes["href"].Value.Contains("Gem")
                        && !link.Attributes["href"].Value.Contains("_Pass"))
                    {
                        if(treasure.Contains == null) treasure.Contains = new List<Item>();
                        
                        Item existItem = CheckExist(link.Attributes["href"].Value);
                        if (existItem == null)
                        {
                            Item newUndefinedItem = new Item(
                                Item.Class.Undefined, 
                                link.Attributes["href"].Value,
                                Item.Chance.UltraRare,
                                treasure
                            );

                            treasure.Contains.Add(newUndefinedItem);
                            items.Add(newUndefinedItem);
                        }
                        else
                        {
                            treasure.Contains.Add(existItem);
                            existItem.DropChance = Item.Chance.UltraRare;
                        }

                        lastSetOrItemURL = link.Attributes["href"].Value;
                    }
                }
            }
        }

        UpdateProgress();
    }

    void ParseSet(Item set)
    {
        set.Type = Item.Class.Set;
        set.Name = _doc.DocumentNode.SelectSingleNode("//table//tr[1]/td").InnerText.Trim();
        
        string rare;
        HtmlNode possibleRare1 = _doc.DocumentNode.SelectSingleNode("//table//tr//td//div//a//span//b");//*[@id="mw-content-text"]/table[1]/tbody/tr[4]/td[2]/div/a/span/b
        HtmlNode possibleRare2 = _doc.DocumentNode.SelectSingleNode("//table//tr[4]//td[2]");
        
        if (possibleRare1.InnerText != "")
            rare = possibleRare1.InnerText;
        else if (possibleRare2.InnerText != "")
            rare = possibleRare2.InnerText;
        else
            rare = "";

        // set.Rare = rare.After(": ");
        set.Rare = rare;
        // set.Rare = _doc.DocumentNode.SelectSingleNode("//table//tr[4]//span//b").InnerText.Trim();
        
        set.ImageURL = _doc.DocumentNode.SelectSingleNode("//table//tr[2]/td/a/img").Attributes["src"].Value ?? "NULL";
        StartCoroutine(LoadImage(set));
        //*[@id="mw-content-text"]/table[1]/tbody/tr[4]/td[2]/div/a/span/b

        /* Parse itemsurls */
        HtmlNodeCollection h2 = _doc.DocumentNode.SelectNodes("//h2//span[@id]");
        HtmlNodeCollection h3 = _doc.DocumentNode.SelectNodes("//h3//span[@id]");
        HtmlNodeCollection h4 = _doc.DocumentNode.SelectNodes("//h4//span[@id]");

        // HtmlNodeCollection allContent = new HtmlNodeCollection(h2[0]);
        HtmlNodeCollection allContent = new HtmlNodeCollection(null);

        if (h2 != null)
            for (int i = 0; i < h2.Count; i++)
            {
                allContent.Add(h2[i]);
            }

        if (h3 != null)
            for (int i = 0; i < h3.Count; i++)
            {
                allContent.Add(h3[i]);
            }

        if (h4 != null)
            for (int i = 0; i < h4.Count; i++)
            {
                allContent.Add(h4[i]);
            }

        /*Sorting allContent*/
        for (int i = 0; i < allContent.Count; i++)
        {
            for (int j = i + 1; j < allContent.Count; j++)
            {
                if (allContent[j].Line < allContent[i].Line)
                {
                    var temp = allContent[i];
                    allContent[i] = allContent[j];
                    allContent[j] = temp;
                }
            }
        }

        allContent.Add(_doc.DocumentNode.SelectSingleNode("//table[@class='navbox']"));

        HtmlNodeCollection allLinks = _doc.DocumentNode.SelectNodes("//div/div/a");

        string lastUndefinedItemURL = "";

        for (int i = 0; i < allContent.Count; i++)
        {
            if (allContent[i].InnerText == "Set Items"
                || allContent[i].InnerText == "Set items"
                || allContent[i].InnerText == "Set Item"
                || allContent[i].InnerText == "Set item"
                || allContent[i].InnerText == "Contents")
            {
                if (set.Contains == null)
                    set.Contains = new List<Item>();

                foreach (var link in allLinks)
                {
                    if (link.Line > allContent[i].Line
                        && link.Line < allContent[i + 1].Line
                        && link.Attributes["href"].Value != lastUndefinedItemURL
                        && !link.Attributes["href"].Value.Contains("?version=")
                        && !link.Attributes["href"].Value.Contains("index.php")
                        && !link.Attributes["href"].Value.Contains("Compendium")
                        && !link.Attributes["href"].Value.Contains("Random")
                        && !link.Attributes["href"].Value.Contains("Upgrade")
                        && !link.Attributes["href"].Value.Contains("Gem")
                        && !link.Attributes["href"].Value.Contains("_Pass"))
                    {
                        Item existItem = CheckExist(link.Attributes["href"].Value);
                        if (existItem == null)
                        {
                            Item newUndefinedItem = new Item(
                                Item.Class.Undefined, 
                                link.Attributes["href"].Value,
                                set
                            );

                            set.Contains.Add(newUndefinedItem);
                            items.Add(newUndefinedItem);
                        }
                        else
                        {
                            set.Contains.Add(existItem);
                        }

                        lastUndefinedItemURL = link.Attributes["href"].Value;
                    }
                }
            }
        }

        UpdateProgress();
    }

    void ParseItem(Item item)
    {
        item.Type = Item.Class.Item;
        item.Name = _doc.DocumentNode.SelectSingleNode("//table[@class]//tr[1]/td").InnerText.Trim();
        item.Rare = _doc.DocumentNode.SelectSingleNode("//table[@class]//tr//td//div//a//span//b").InnerText.Trim();
        item.ImageURL = _doc.DocumentNode.SelectSingleNode("//table[@class]//tr[2]/td/a/img").Attributes["src"].Value;
        item.Slot = _doc.DocumentNode.SelectSingleNode("//table//tr[5]//td").InnerText.After(": ");

        StartCoroutine(LoadImage(item));

        UpdateProgress();
    }

    public void ParseAllUndefinedItems()
    {
        StartCoroutine(ParseAllUndefinedItemsCoroutine());
    }

    public IEnumerator ParseAllUndefinedItemsCoroutine()
    {
        List<Item> undefinedItems = items.Where( item => item.Type == Item.Class.Undefined )
            .OrderBy( item => item.Id )
            .ToList();
        print("parsing " + undefinedItems.Count);

        maxProgressBarValue += undefinedItems.Count;

        foreach(Item undefinedItem in undefinedItems)
        {
            ParseUndefinedItem(undefinedItem);
            
            yield return null;
        }

        if (parseSetsContentToggle.isOn)
            ParseAllUndefinedItemsInSets();
    }

    public void ParseAllUndefinedItemsInSets()
    {
        StartCoroutine(ParseAllUndefinedItemsCoroutine());
    }

    public IEnumerator ParseAllUndefinedItemsInSetsCoroutine()
    {
        List<Item> undefinedItems = items.Where( item => item.Type == Item.Class.Undefined )
            .OrderBy( item => item.Id )
            .ToList();
        print("parsing " + undefinedItems.Count);

        maxProgressBarValue += undefinedItems.Count;

        foreach(Item undefinedItem in undefinedItems)
        {
            ParseUndefinedItem(undefinedItem);
            
            yield return null;
        }
    }

    void ParseUndefinedItem(Item undefinedItem)
    {
        _webGet = new HtmlWeb();
        _doc = _webGet.Load(wikiSiteURL + undefinedItem.Url);

        // HtmlNode type = _doc.DocumentNode.SelectSingleNode("//table[@class]//tr[2]/td");

        bool set = false;

        // /* Parse itemsurls */
        // HtmlNodeCollection h2 = _doc.DocumentNode.SelectNodes("//h2//span[@id]");
        // HtmlNodeCollection h3 = _doc.DocumentNode.SelectNodes("//h3//span[@id]");
        // HtmlNodeCollection h4 = _doc.DocumentNode.SelectNodes("//h4//span[@id]");

        // HtmlNodeCollection allContent = new HtmlNodeCollection(null);

        // if (h2 != null)
        //     for (int i = 0; i < h2.Count; i++)
        //     {
        //         allContent.Add(h2[i]);
        //     }

        // if (h3 != null)
        //     for (int i = 0; i < h3.Count; i++)
        //     {
        //         allContent.Add(h3[i]);
        //     }

        // if (h4 != null)
        //     for (int i = 0; i < h4.Count; i++)
        //     {
        //         allContent.Add(h4[i]);
        //     }

        // /*Sorting allContent*/
        // for (int i = 0; i < allContent.Count; i++)
        // {
        //     for (int j = i + 1; j < allContent.Count; j++)
        //     {
        //         if (allContent[j].Line < allContent[i].Line)
        //         {
        //             var temp = allContent[i];
        //             allContent[i] = allContent[j];
        //             allContent[j] = temp;
        //         }
        //     }
        // }

        // // allContent.Add(_doc.DocumentNode.SelectSingleNode("//table[@class='navbox']"));

        // foreach(HtmlNode node in allContent)
        // {
        //     if (node.InnerText.Contains("Bundle")
        //     || node.InnerText.Contains("Pack")
        //     || node.InnerText.Contains("Set"))
        //     {
        //         set = true;
        //         print(node.InnerText);
        //         break;
        //     }
        // }
        

        string slot;
        HtmlNode possibleSlot1 = _doc.DocumentNode.SelectSingleNode("//table//tr[4]//td");
        HtmlNode possibleSlot2 = _doc.DocumentNode.SelectSingleNode("//table//tr[5]//td");
        
        if (possibleSlot1.InnerText.Contains("Slot"))
            slot = possibleSlot1.InnerText;
        else if (possibleSlot2.InnerText.Contains("Slot"))
            slot = possibleSlot2.InnerText;
        else
            slot = "";

        if (slot.Count() != 0)
        {
            if (slot.Contains("Bundle")
            || slot.Contains("Pack")
            || slot.Contains("Weapon Pack"))
            {
                set = true;
                print(slot);
            }
            else
            {
                set = false;
            }
        }   
        else
        {
            set = true;
        }         

        if (set)
        {
            //                Console.WriteLine(type.InnerText);
            // if (type.InnerText.Contains("Bundle")
                // || type.InnerText.Contains("Pack"))
            // {
                ParseSet(undefinedItem);
            // }
            // else
            // {
                // ParseItem(undefinedItem);
            // }
        }
        else
        {
            ParseItem(undefinedItem);
        }
        // else
        // {
        //     print("ХЗ что это вообще такое");
        // }
    }

    void DrawItem(int id)
    {
        var item = items[id];
        
        switch(item.Type)
        {
            case Item.Class.Treasure:
                TreasureView tv = null;

                foreach(TreasureView treasureView in treasuresViews)
                {
                    if (treasureView.id.text == item.Id.ToString())
                    {
                        tv = treasureView;
                    }
                }

                if(tv == null)
                {
                    GameObject treasureViewGameObject = Instantiate(treasurePrefab);
                    treasureViewGameObject.name = item.Id.ToString() /*+ " " + item.Name*/;
                    treasureViewGameObject.transform.SetParent(treasuresHolderObject.transform);
                    TreasureView treasureView = treasureViewGameObject.GetComponent<TreasureView>();
                    treasuresViews.Add(treasureView);
                    DrawTreasureView(treasureView, item);
                }
                else
                {
                    DrawTreasureView(tv, item);
                }
                break;
            case Item.Class.Set:
                SetView sv = null;

                foreach(SetView setView in setsViews)
                {
                    if (setView.id.text == item.Id.ToString())
                    {
                        sv = setView;
                    }
                }

                if(sv == null)
                {
                    GameObject setViewGameObject = Instantiate(setPrefab);
                    setViewGameObject.name = item.Id.ToString() /*+ " " + item.Name*/;
                    setViewGameObject.transform.SetParent(setsHolderObject.transform);
                    SetView setView = setViewGameObject.GetComponent<SetView>();
                    setsViews.Add(setView);
                    DrawSetView(setView, item);
                }
                else
                {
                    DrawSetView(sv, item);
                }
                break;
            case Item.Class.Item:
                ItemView iv = null;

                foreach(ItemView itemView in itemsViews)
                {
                    if (itemView.id.text == item.Id.ToString())
                    {
                        iv = itemView;
                    }
                }

                if(iv == null)
                {
                    GameObject itemViewGameObject = Instantiate(itemPrefab);
                    itemViewGameObject.name = item.Id.ToString() /*+ " " + item.Name*/;
                    itemViewGameObject.transform.SetParent(itemsHolderObject.transform);
                    ItemView itemView = itemViewGameObject.GetComponent<ItemView>();
                    itemsViews.Add(itemView);
                    DrawItemView(itemView, item);
                }
                else
                {
                    DrawItemView(iv, item);
                }
                break;
        }
    }

    public void DrawTables()
    {
        DrawTreasuresTable();
        DrawSetsTable();
        DrawItemsTable();
    }

    public void DrawTreasuresTable()
    {
        List<Item> treasures = items.Where( item => item.Type == Item.Class.Treasure )
            .OrderBy( item => item.Id )
            .ToList();
        
        foreach(Item treasure in treasures)
        {
            DrawItem(treasure.Id);
        }
    }

    public void DrawSetsTable()
    {
        List<Item> sets = items.Where( item => item.Type == Item.Class.Set )
            .OrderBy( item => item.Id )
            .ToList();
        
        foreach(Item set in sets)
        {
            DrawItem(set.Id);
        }
    }

    public void DrawItemsTable()
    {
        List<Item> gameItems = items.Where( item => item.Type == Item.Class.Item )
            .OrderBy( item => item.Id )
            .ToList();
        
        foreach(Item item in gameItems)
        {
            DrawItem(item.Id);
        }
    }

    void DrawTreasureView(TreasureView treasureView, Item item)
    {
        treasureView.id.text = item.Id.ToString() ?? null;
        treasureView.name.text = item.Name ?? null;
        treasureView.rare.text = item.Rare ?? null;
        treasureView.cost.text = item.Cost ?? null;
        treasureView.image.texture = item.imageTexture ?? null;
        treasureView.url.text = item.Url ?? null;
        
        //Regular
        string regular = "";
        if (item.Contains != null)
        {
            foreach(Item treasure in item.Contains.Where( treasure => treasure.DropChance == Item.Chance.Regular ).ToList())
                regular += treasure.Id + " " + treasure.Url + "\r\n";
            treasureView.regular.text = regular; 
        }
        else
            treasureView.regular.text = "NULL";

        //Rare
        string rare = "";
        if (item.Contains != null)
        {
            foreach(Item treasure in item.Contains.Where( treasure => treasure.DropChance == Item.Chance.Rare ).ToList())
                rare += treasure.Id + " " + treasure.Url + "\r\n";
            treasureView.rareItems.text = rare; 
        }
        else
            treasureView.rareItems.text = "NULL";

        //Very Rare
        string veryRare = "";
        if (item.Contains != null)
        {
            foreach(Item treasure in item.Contains.Where( treasure => treasure.DropChance == Item.Chance.VeryRare ).ToList())
                veryRare += treasure.Id + " " + treasure.Url + "\r\n";
            treasureView.veryRare.text = veryRare; 
        }
        else
            treasureView.veryRare.text = "NULL";

        //Extremely Rare
        string extremelyRare = "";
        if (item.Contains != null)
        {
            foreach(Item treasure in item.Contains.Where( treasure => treasure.DropChance == Item.Chance.ExtremelyRare ).ToList())
                extremelyRare += treasure.Id + " " + treasure.Url + "\r\n";
            treasureView.extremelyRare.text = extremelyRare; 
        }
        else
            treasureView.extremelyRare.text = "NULL";

        //Ultra Rare
        string ultraRare = "";
        if (item.Contains != null)
        {
            foreach(Item treasure in item.Contains.Where( treasure => treasure.DropChance == Item.Chance.UltraRare ).ToList())
                ultraRare += treasure.Id + " " + treasure.Url + "\r\n";
            treasureView.ultraRare.text = ultraRare; 
        }
        else
            treasureView.ultraRare.text = "NULL";
    }

    void DrawSetView(SetView setView, Item set)
    {
        setView.id.text = set.Id.ToString() ?? null;
        setView.name.text = set.Name ?? null;
        setView.rare.text = set.Rare ?? null;
        setView.cost.text = set.Cost ?? null;
        setView.image.texture = set.imageTexture ?? null;
        setView.url.text = set.Url ?? null;
        
        //Content
        string content = "";
        if (set.Contains != null)
        {
            foreach(Item treasure in set.Contains)
                content += treasure.Id + " " + treasure.Url + "\r\n";
            setView.contains.text = content; 
        }
        else
            setView.contains.text = "NULL";

        //Contains in
        string containsIn = "";
        if (set.ContainsInside != null)
        {
            foreach(Item treasure in set.ContainsInside)
                containsIn += treasure.Id + " " + treasure.Url + "\r\n";
            setView.containsIn.text = containsIn; 
        }
        else
            setView.containsIn.text = "NULL";
    }

    void DrawItemView(ItemView itemView, Item item)
    {
        itemView.id.text = item.Id.ToString() ?? null;
        itemView.name.text = item.Name ?? null;
        itemView.rare.text = item.Rare ?? null;
        itemView.cost.text = item.Cost ?? null;
        itemView.image.texture = item.imageTexture ?? null;
        itemView.url.text = item.Url ?? null;
        itemView.slot.text = item.Slot ?? null;

        //Contains in
        string containsIn = "";
        if (item.ContainsInside != null)
        {
            foreach(Item treasure in item.ContainsInside)
                containsIn += treasure.Id + " " + treasure.Url + "\r\n";
            itemView.containsIn.text = containsIn; 
        }
        else
            itemView.containsIn.text = "NULL";
    }

    IEnumerator LoadImage(Item item)
    {
        WWW www = new WWW(item.ImageURL);
        yield return www;

        if (!string.IsNullOrEmpty(www.error))
        {
            Debug.Log(www.error);
            StartCoroutine(LoadImage(item));
        }
        item.imageTexture = www.texture;

        switch(item.Type)
        {
            case Item.Class.Treasure:
                DrawTreasuresTable();
                break;
            case Item.Class.Set:
                DrawSetsTable();
                break;
            case Item.Class.Item:
                DrawItemsTable();
                break;
        }
    }

    IEnumerator LoadImage(Item item, string url)
    {
        WWW www = new WWW(url);
        yield return www;

        item.imageTexture = www.texture;

        switch(item.Type)
        {
            case Item.Class.Treasure:
                DrawTreasuresTable();
                break;
            case Item.Class.Set:
                DrawSetsTable();
                break;
            case Item.Class.Item:
                DrawItemsTable();
                break;
        }
    }

    Item CheckExist(string url)
    {
        Item existItem = items.Find( item => item.Url == url);

        if (existItem != null)
        {
            print("Item already exist");
            return existItem;
        }    
        else
            return null;
    }

    void UpdateProgress()
    {
        curProgressBarValue++;
        linearProgressIndicator.SetProgress(curProgressBarValue/maxProgressBarValue);
        progressBarValueText.text = curProgressBarValue + "/" + maxProgressBarValue;
    }

    public void Save()
    {
        List<Item> definedItems = items.Where( item => item.Type == Item.Class.Treasure 
            || item.Type == Item.Class.Set
            || item.Type == Item.Class.Item).ToList();
        
        string json = JsonConvert.SerializeObject(definedItems,//items, 
            // Formatting.Indented, 
            new JsonSerializerSettings { 
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            });
        print(json);

        File.WriteAllText(Application.dataPath + "/" + itemsFileName, json);

        foreach(Item item in definedItems)
        {
            TextureScale.Bilinear(item.imageTexture, 256, 170);
            File.WriteAllBytes(Application.dataPath + "/items/" + item.Id + ".png", item.imageTexture.EncodeToPNG());
        }

        print("Saved");
    }

    public void Load()
    {
        if (items == null) items = new List<Item>();
        else
        {
            items.Clear();
            DrawTables();
        }
        
        string json = File.ReadAllText(Application.dataPath + "/" + itemsFileName);

        items = JsonConvert.DeserializeObject<List<Item>>(json,
            new JsonSerializerSettings { 
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            });

        List<Item> definedItems = items.Where( item => item.Type == Item.Class.Treasure 
            || item.Type == Item.Class.Set
            || item.Type == Item.Class.Item).ToList();

        foreach(Item item in definedItems)
        {
            item.imageTexture = new Texture2D(256, 171); 
            item.imageTexture.LoadImage(File.ReadAllBytes(Application.dataPath + "/items/" + item.Id + ".png"));
        }

        // StartCoroutine(LoadItemsImages());
        
        DrawTables();
        print("Loaded");
    }

    IEnumerator LoadItemsImages()
    {
        foreach(Item item in items)
        {
            StartCoroutine(LoadImage(item, Application.dataPath + "/items/" + item.Id + ".png"));
            yield return null;
        }
    }

    public class Item
    {
        public static int Count = 0;
        public int Id { get; set; }
        public string Name { get; set; }
        public string Rare { get; set; }
        public string Cost { get; set; } 
        [JsonIgnoreAttribute]
        public string ImageURL { get; set; }
        [JsonIgnoreAttribute]
        public Texture2D imageTexture { get; set; }
        public string Url { get; set; }
        public Class Type { get; set; }
        public List<Item> Contains { get; set; }
        public List<Item> ContainsInside { get; set; }
        public Chance DropChance { get; set; }
        public string Slot { get; set; }

        public enum Class
        {
            Undefined,
            Treasure,
            Set,
            Item
        }
        
        public enum Chance
        {
            Regular,
            Rare,
            VeryRare,
            ExtremelyRare,
            UltraRare,
        }

        public Item()
        {
            this.Id = -1;

            Count++;
        }

        public Item(Class itemType)
        {
            this.Type = itemType;
            this.Id = Count;

            Count++;
        }

        public Item(Class itemType, string url, Chance dropChance, Item containsInsideItem)
        {
            this.Type = itemType;
            this.Id = Count;
            this.Url = url;
            this.DropChance = dropChance;
            this.ContainsInside = new List<Item>();
            this.ContainsInside.Add(containsInsideItem);

            Count++;
        }

        public Item(Class itemType, string url, Item containsInsideItem)
        {
            this.Type = itemType;
            this.Id = Count;
            this.Url = url;
            this.ContainsInside = new List<Item>();
            this.ContainsInside.Add(containsInsideItem);

            Count++;
        }
    } 
}

static class SubstringExtensions
    {
        /// <summary>
        /// Get string value between [first] a and [last] b.
        /// </summary>
        public static string Between(this string value, string a, string b)
        {
            int posA = value.IndexOf(a);
            //            int posB = value.LastIndexOf(b);
            int posB = value.IndexOf(b);

            if (posA == -1)
            {
                return "";
            }
            if (posB == -1)
            {
                return "";
            }
            int adjustedPosA = posA + a.Length;
            if (adjustedPosA >= posB)
            {
                return "";
            }
            return value.Substring(adjustedPosA, posB - adjustedPosA);
        }

        /// <summary>
        /// Get string value after [first] a.
        /// </summary>
        public static string Before(this string value, string a)
        {
            int posA = value.IndexOf(a);
            if (posA == -1)
            {
                return "";
            }
            return value.Substring(0, posA);
        }

        /// <summary>
        /// Get string value after [last] a.
        /// </summary>
        public static string After(this string value, string a)
        {
            int posA = value.LastIndexOf(a);
            if (posA == -1)
            {
                return "";
            }
            int adjustedPosA = posA + a.Length;
            if (adjustedPosA >= value.Length)
            {
                return "";
            }
            return value.Substring(adjustedPosA);
        }
    }