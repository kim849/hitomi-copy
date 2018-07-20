﻿/* Copyright (C) 2018. Hitomi Parser Developers */

using Hitomi_Copy;
using Hitomi_Copy.Data;
using Hitomi_Copy_2;
using Hitomi_Copy_2.Analysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Hitomi_Copy_3
{
    public partial class CustomArtistRecommendation : Form
    {
        public CustomArtistRecommendation()
        {
            InitializeComponent();
        }

        private void CustomArtistRecommendation_Load(object sender, EventArgs e)
        {
            ColumnSorter.InitListView(lvCustomTag);
            ColumnSorter.InitListView(lvArtists);
            
            Dictionary<string, int> tags_map = new Dictionary<string, int>();

            foreach (var log in HitomiLog.Instance.GetEnumerator().Where(log => log.Tags != null))
            {
                foreach (var tag in log.Tags)
                {
                    if (HitomiSetting.Instance.GetModel().UsingOnlyFMTagsOnAnalysis &&
                        !tag.StartsWith("female:") && !tag.StartsWith("male:")) continue;
                    if (tags_map.ContainsKey(HitomiCommon.LegalizeTag(tag)))
                        tags_map[HitomiCommon.LegalizeTag(tag)] += 1;
                    else
                        tags_map.Add(HitomiCommon.LegalizeTag(tag), 1);
                }
            }

            var list = tags_map.ToList();
            list.Sort((a, b) => b.Value.CompareTo(a.Value));

            List<ListViewItem> lvil = new List<ListViewItem>();
            foreach (var item in list)
                lvil.Add(new ListViewItem(new string[] { item.Key, item.Value.ToString() }));
            lvCustomTag.Items.Clear();
            lvCustomTag.Items.AddRange(lvil.ToArray());
            
            lvil.Clear();
            var list2 = HitomiAnalysis.Instance.Rank.ToList();
            for (int i = 0; i < list2.Count; i++)
            {
                lvil.Add(new ListViewItem(new string[] {
                    (i + 1).ToString(),
                    list2[i].Item1,
                    list2[i].Item2.ToString(),
                    list2[i].Item3
                }));
            }
            lvArtists.Items.Clear();
            lvArtists.Items.AddRange(lvil.ToArray());

            HitomiAnalysis.Instance.UserDefined = true;
        }

        private void lvCustomTag_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                foreach (ListViewItem eachItem in lvCustomTag.SelectedItems)
                    lvCustomTag.Items.Remove(eachItem);
            }
            else if (e.Control && e.KeyCode == Keys.A)
            {
                foreach (ListViewItem eachItem in lvCustomTag.Items)
                    eachItem.Selected = true;
            }
        }

        private async void bUpdate_ClickAsync(object sender, EventArgs e)
        {
            HitomiAnalysis.Instance.CustomAnalysis.Clear();
            
            foreach (var lvi in lvCustomTag.Items.OfType<ListViewItem>())
                HitomiAnalysis.Instance.CustomAnalysis.Add(new Tuple<string, int>(lvi.SubItems[0].Text, Convert.ToInt32(lvi.SubItems[1].Text)));
            
            await Task.Run(() => HitomiAnalysis.Instance.Update());
            (Application.OpenForms[0] as frmMain).UpdateNewStatistics();

            List<ListViewItem> lvil = new List<ListViewItem>();
            var list2 = HitomiAnalysis.Instance.Rank.ToList();
            for (int i = 0; i < list2.Count; i++)
            {
                lvil.Add(new ListViewItem(new string[] {
                    (i + 1).ToString(),
                    list2[i].Item1,
                    list2[i].Item2.ToString(),
                    list2[i].Item3
                }));
            }
            lvArtists.Items.Clear();
            lvArtists.Items.AddRange(lvil.ToArray());
        }

        private void bAddTag_Click(object sender, EventArgs e)
        {
            (new CARTag(this)).ShowDialog();
        }

        private void bAddArtistTag_Click(object sender, EventArgs e)
        {
            (new CARArtist(this)).ShowDialog();
        }

        public void RequestAddTags(string tags, string score)
        {
            foreach (var tag in tags.Trim().Split(' '))
            {
                if (!lvCustomTag.Items.OfType<ListViewItem>().ToList().Any(x => {
                    if (x.SubItems[0].Text == tag)
                        x.SubItems[1].Text = score;
                    return x.SubItems[0].Text == tag; }))
                {
                    lvCustomTag.Items.Add(new ListViewItem(new string[] { tag, score }));
                }
            }
        }

        public void RequestAddArists(string artists, string score)
        {
            Dictionary<string, int> tags = new Dictionary<string, int>();

            foreach (var artist in artists.Trim().Split(' '))
            {
                foreach (var data in HitomiData.Instance.metadata_collection)
                {
                    if (!HitomiSetting.Instance.GetModel().RecommendLanguageALL)
                    {
                        string lang = data.Language;
                        if (data.Language == null) lang = "N/A";
                        if (HitomiSetting.Instance.GetModel().Language != "ALL" &&
                            HitomiSetting.Instance.GetModel().Language != lang) continue;
                    }
                    if (data.Artists != null && data.Tags != null && data.Artists.Contains(artist))
                    {
                        foreach (var tag in data.Tags)
                        {
                            if (tags.ContainsKey(tag))
                                tags[tag] = tags[tag] + 1;
                            else
                                tags.Add(tag, 1);
                        }
                    }
                }
            }

            var list = tags.ToList();
            list.Sort((a, b) => b.Value.CompareTo(a.Value));

            foreach (var tag in list)
            {
                if (!lvCustomTag.Items.OfType<ListViewItem>().ToList().Any(x => {
                    if (x.SubItems[0].Text == tag.Key)
                        x.SubItems[1].Text = (tag.Value * Convert.ToInt32(score)).ToString();
                    return x.SubItems[0].Text == tag.Key;
                }))
                {
                    lvCustomTag.Items.Add(new ListViewItem(new string[] { tag.Key, (tag.Value * Convert.ToInt32(score)).ToString() }));
                }
            }
        }

        private void lvArtists_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (lvArtists.SelectedItems.Count > 0)
            {
                (new frmArtistInfo(lvArtists.SelectedItems[0].SubItems[1].Text)).Show();
            }
        }
    }
}