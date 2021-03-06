﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StoryBookEditor
{
    public enum TransitionTypes
    {
        None,
        Fade,
        Slide,
    }

    public enum SlideDirections
    {
        Left,
        Right,
        Top,
        Buttom,
    }

    [ExecuteInEditMode]
    public class ScreenManager : MonoBehaviour
    {
        #region All Transition User Editable Properties
        protected TransitionTypes SelectedTransitionType = TransitionTypes.None;
        protected Texture2D _fadeImage;
        protected int InSpeedMs = 500;

        public OnClickDelegate ItemClickDelegate { get; set; }
        protected SpriteRenderer SpriteRenderer { get; set; }
        protected Animation Animation { get; set; }
        protected float CurrentAnimationTime { get; set; }
        protected StoryBranchModel AnimatingBranch { get; set; }
        protected float LastTime { get; set; }
        protected Sprite _pageBg;

        protected IEnumerable<StoryBranchModel> _branches;
        public StoryBookModel StoryBookInstance { get; set; }
        protected bool _isUIDirty;

        protected const float WIDTH_PERCENTAGE = .0625f;
        protected const float HEIGHT_PERCENTAGE = .0833f;

        protected const string CURRENT_SPRITE = "CURRENT_SPRITE";
        protected const string NEXT_SPRITE = "NEXT_SPRITE";

        #region Sliding Unique
        protected int SlideDirection = 0;
        #endregion
        #endregion

        #region FadeParameters
        protected int DrawDepth = -1000;
        private float fadeAlpha = -1f;
        protected int FadeDir = 0;
        #endregion
        #region Slide Parameters
        GameObject _currentPageImage;
        GameObject _nextPageImage;
        #endregion

        /// <summary>
        /// On Enable, make sure all of the components are loaded and load the fade to black image
        /// On Enable should fire evertime we go from tombstoned or turn off to running
        /// </summary>
        public void OnEnable()
        {
            if (SpriteRenderer == null && (SpriteRenderer = GetComponent<SpriteRenderer>()) == null)
                SpriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            if (Animation == null && (Animation = GetComponent<Animation>()) == null)
                Animation = gameObject.AddComponent<Animation>();

            _fadeImage = Resources.Load<Texture2D>("FadeImg");
        }

        public float BeginFade(int direction = -1)
        {
            SelectedTransitionType = TransitionTypes.Fade;
            FadeDir = direction;
            if (direction == -1)
                fadeAlpha = 1f;
            else
                fadeAlpha = 0f;

            return InSpeedMs / 1000;
        }

        private double _timeoutBucket;

        public event EventHandler<TEventArg<StoryBranchModel>> TransitionComplete;

        protected void OnClickHandler(StoryBranchModel id)
        {
            if (ItemClickDelegate != null)
                ItemClickDelegate(id);
        }

        public void BeginSlide(Sprite startObj, Sprite endObj, SlideDirections dir = SlideDirections.Left)
        {
            if (startObj == null)
                return;

            SelectedTransitionType = TransitionTypes.Slide;

            _currentPageImage = GameObject.Find(CURRENT_SPRITE);
            var spriteRender = transform.GetComponent<SpriteRenderer>();
            SpriteRenderer sr;

            if (_currentPageImage == null)
            {
                _currentPageImage = new GameObject();
                _currentPageImage.name = CURRENT_SPRITE;
                _currentPageImage.transform.parent = transform.parent;
                sr = _currentPageImage.AddComponent<SpriteRenderer>();
            }
            else
            {
                sr = _currentPageImage.GetComponent<SpriteRenderer>();
            }
            sr.sortingOrder = 2;
            sr.sprite = startObj;
            _currentPageImage.transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, transform.localPosition.z-0.9f);
            _currentPageImage.transform.localScale = transform.localScale;

            if (endObj)
            {
                _nextPageImage = GameObject.Find(NEXT_SPRITE);
                if (_nextPageImage == null)
                {
                    _nextPageImage = new GameObject();
                    _nextPageImage.name = NEXT_SPRITE;
                    _nextPageImage.transform.parent = transform.parent;
                    sr = _nextPageImage.AddComponent<SpriteRenderer>();
                }
                else
                {
                    sr = _nextPageImage.GetComponent<SpriteRenderer>();
                }
                sr.sortingOrder = 2;
                sr.sprite = endObj;
                _nextPageImage.transform.localScale = transform.localScale;
                float location = (spriteRender.bounds.size.x + sr.bounds.size.x) / 2.0f;
                _nextPageImage.transform.localPosition = new Vector3(location, 0f, -0.1f);
            }
        }

        protected void setupui()
        {
            var camera = Camera.main;
            if (camera == null && Camera.allCamerasCount > 0)
            {
                Camera.allCameras[0].tag = "MainCamera";
                camera = Camera.allCameras[0];
            }

            if (camera == null || Screen.height < 1 || Screen.width < 1)
            {
                return;
            }

            if (!camera.orthographic)
                camera.orthographic = true;
            camera.transform.position = new Vector3(0, 0, -10);

            var worldScreenHeight = camera.orthographicSize * 2.0;

            //resize accordingly
            float scalex = (float)((worldScreenHeight / camera.pixelHeight * camera.pixelWidth) / SpriteRenderer.sprite.bounds.size.x);
            float scaley = (float)(worldScreenHeight / SpriteRenderer.sprite.bounds.size.y);
            SpriteRenderer.transform.localScale = new Vector3((float)Math.Min(scalex, scaley), (float)Math.Min(scalex, scaley), 1);

            if (_pageBg != null && StoryBookInstance != null)
            {
                #region Display Branch Info
                var halfX = SpriteRenderer.sprite.bounds.size.x / 2f;
                var halfY = SpriteRenderer.sprite.bounds.size.y / 2f;
                var sectionX = SpriteRenderer.sprite.bounds.size.x * WIDTH_PERCENTAGE;
                var sectionY = SpriteRenderer.sprite.bounds.size.y * HEIGHT_PERCENTAGE;

                if (_branches != null && _branches.Any())
                {
                    foreach (var branch in _branches)
                    {
                        if (branch.GameObj != null && (Application.isEditor || StoryBookInstance.ShowBranch(branch)))
                        {
                            var sR = branch.GameObj.AddComponent<SpriteRenderer>();
                            sR.sprite = branch.ImageSprite;

                            if (sR.sprite != null)
                            {
                                scalex = (float)(sectionX / sR.sprite.bounds.size.x);
                                scaley = (float)(sectionY / sR.sprite.bounds.size.y);
                                if (scalex > 0f && scaley > 0f)
                                {
                                    sR.transform.localScale = new Vector3(scalex * branch.ItemSize.x, scaley * branch.ItemSize.y, 1f);
                                    sR.sortingOrder = 1;
                                    //set branch location
                                    branch.GameObj.transform.localPosition = new Vector3(((sectionX * branch.ItemLocation.x) - halfX) + ((sectionX * branch.ItemSize.x) / 2f), ((sectionY * branch.ItemLocation.y) - halfY) + ((sectionY * branch.ItemSize.y) / 2f), 0f);
                                }

                                var animator = branch.GameObj.AddComponent<Animation>();

                                if (animator.clip != null)
                                {
                                    animator.RemoveClip(animator.clip);
                                    animator.clip = null;
                                }

                                if (branch.CurrentAnimation != null)
                                {
                                    animator.AddClip(branch.CurrentAnimation, branch.Animation);
                                    animator.clip = branch.CurrentAnimation;
                                    if (branch.IsLooping)
                                    {
                                        animator.Play(PlayMode.StopAll);
                                    }
                                }
                            }
                        }
                    }
                }
                #endregion
            }
            _isUIDirty = false;
        }

        public void OnGUI()
        {
            if (_isUIDirty && Camera.allCamerasCount > 0)
            {
                setupui();
            }

            _timeoutBucket += Time.deltaTime;

            if (_timeoutBucket < .001f)
            {
                System.Threading.Thread.SpinWait(60);
                return;
            }
            //Handle the fade transition
            if (SelectedTransitionType == TransitionTypes.Fade && _fadeImage != null && fadeAlpha <= 1.0f && fadeAlpha >= 0.0f)
            {
                fadeAlpha += (float)(FadeDir * (1000f / InSpeedMs) * _timeoutBucket);
                fadeAlpha = Mathf.Clamp01(fadeAlpha);

                GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, fadeAlpha);
                GUI.depth = DrawDepth;
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _fadeImage);

                if (fadeAlpha == 0.00f || fadeAlpha == 1.00f)
                {
                    fadeAlpha = -1;
                    SelectedTransitionType = TransitionTypes.None;
                    if (TransitionComplete != null)
                        TransitionComplete(this, new TEventArg<StoryBranchModel>(AnimatingBranch));
                }
            }
            else if(SelectedTransitionType == TransitionTypes.Fade)
            {
                SelectedTransitionType = TransitionTypes.None;
                if (TransitionComplete != null)
                    TransitionComplete(this, new TEventArg<StoryBranchModel>(AnimatingBranch));
            }
            //Handle Slide Transition
            else if (SelectedTransitionType == TransitionTypes.Slide && _nextPageImage != null)
            {
                Vector3 delta = transform.localPosition - _nextPageImage.transform.localPosition;
                float slideOffset = (float)((1000d / InSpeedMs) * _timeoutBucket);

                if (Math.Abs(delta.x) <= slideOffset && Math.Abs(delta.y) <= slideOffset)
                {
                    SelectedTransitionType = TransitionTypes.None;
                    DestroyImmediate(_nextPageImage);
                    if(_currentPageImage)
                        DestroyImmediate(_currentPageImage);

                    if (TransitionComplete != null)
                        TransitionComplete(this, new TEventArg<StoryBranchModel>(AnimatingBranch));
                }
                else
                {
                    switch (SlideDirection)
                    {
                        case 0: //Left
                            if (_currentPageImage)
                                _currentPageImage.transform.localPosition = _currentPageImage.transform.localPosition - new Vector3(slideOffset, 0f, 0f);
                            _nextPageImage.transform.localPosition = _nextPageImage.transform.localPosition - new Vector3(slideOffset, 0f, 0f);
                            break;
                        case 1://Right
                            if (_currentPageImage)
                                _currentPageImage.transform.localPosition = _currentPageImage.transform.localPosition + new Vector3(slideOffset, 0f, 0f);
                            _nextPageImage.transform.localPosition = _nextPageImage.transform.localPosition + new Vector3(slideOffset, 0f, 0f);
                            break;
                        case 2://Top
                            if (_currentPageImage)
                                _currentPageImage.transform.localPosition = _currentPageImage.transform.localPosition - new Vector3(0f, slideOffset, 0f);
                            _nextPageImage.transform.localPosition = _nextPageImage.transform.localPosition - new Vector3(0f, slideOffset, 0f);
                            break;
                        case 3://Buttom
                            if (_currentPageImage)
                                _currentPageImage.transform.localPosition = _currentPageImage.transform.localPosition + new Vector3(0f, slideOffset, 0f);
                            _nextPageImage.transform.localPosition = _nextPageImage.transform.localPosition + new Vector3(0f, slideOffset, 0f);
                            break;
                    }
                }
            }
            else if(SelectedTransitionType == TransitionTypes.Slide)
            {
                SelectedTransitionType = TransitionTypes.None;
                DestroyImmediate(_nextPageImage);
                if (_currentPageImage)
                    DestroyImmediate(_currentPageImage);

                if (TransitionComplete != null)
                    TransitionComplete(this, new TEventArg<StoryBranchModel>(AnimatingBranch));
            }

            _timeoutBucket = 0;
        }

        /// <summary>
        /// Listens for click event where there are not branches
        /// </summary>
        private void Update()
        {
            if(CurrentAnimationTime > 0)
            {
                var diff = Time.time - LastTime;
                if (diff > CurrentAnimationTime)
                {
                    OnClickHandler(AnimatingBranch);
                    CurrentAnimationTime = 0;
                    LastTime = 0;
                }

                return;
            }

            if (SelectedTransitionType != TransitionTypes.None)
            {
                
            }
            else if (Input.GetMouseButtonDown(0))
            {
                var clickX = (float)Math.Floor(Input.mousePosition.x / (Screen.width * WIDTH_PERCENTAGE));
                var clickY = (float)Math.Floor(Input.mousePosition.y / (Screen.height * HEIGHT_PERCENTAGE));

                var viewableBranches = _branches.Where(x => (StoryBookInstance == null || StoryBookInstance.ShowBranch(x)));
                if(_branches == null || !_branches.Any() || !viewableBranches.Any())
                {
                    //If I have no branches fire default message
                    OnClickHandler(null);
                }
                else
                {
                    var validBranch = viewableBranches.Where(x => !string.IsNullOrEmpty(x.Image) && !x.IsAnyClick() && x.DidClick((int)clickX, (int)clickY)).FirstOrDefault();
                    if (validBranch == null)
                    {
                        validBranch = viewableBranches.Where(x => string.IsNullOrEmpty(x.Image) || x.IsAnyClick()).FirstOrDefault();
                    }
                    if (validBranch != null)
                    {
                        if (!validBranch.IsLooping && validBranch.CurrentAnimation != null)
                        {
                            var animator = validBranch.GameObj.GetComponent<Animation>();
                            AnimatingBranch = validBranch;
                            animator.Play();
                            CurrentAnimationTime = validBranch.AnimationLength;
                            LastTime = Time.time;
                        }
                        else
                        {
                            OnClickHandler(validBranch);
                        }
                    }
                }
            }
        }

        public void UpdateDraw(Sprite pageBackground, AnimationClip clip, IEnumerable<StoryBranchModel> branches)
        {
            if (SpriteRenderer == null)
            {
                Debug.LogError("Unable to load Story Book sprite renderer, please restart unity");
                return;
            }

            if(StoryBookInstance == null)
            {
                Debug.LogError("No story book instance in screen manager.  Drawing all instances of ");
            }

            //ToDo, don't always clean up old branches, only if new content
            //clean out the old branches
            var children = new List<GameObject>();
            foreach (Transform child in transform) children.Add(child.gameObject);
            children.Where(x => x != null).ToList().ForEach(child => DestroyImmediate(child));

            if (pageBackground == null)
            {
                _pageBg = null;
                SpriteRenderer.sprite = null;
                _branches = new List<StoryBranchModel>();
                return;
            }
            if (_pageBg == null || (_pageBg.name != pageBackground.name))
            {
                _pageBg = pageBackground;
                SpriteRenderer.sprite = _pageBg;
                SpriteRenderer.sortingOrder = 0;
            }

            if (Animation.clip != null)
            {
                Animation.RemoveClip(Animation.clip);
                Animation.clip = null;
            }

            if (clip != null && Animation.GetClip(clip.name) == null)
            {
                Animation.AddClip(clip, clip.name);
                Animation.clip = clip;
                Animation.Play(PlayMode.StopAll);
            }

            _branches = branches;
            
            if (branches != null && branches.Any())
            {
                foreach (var branch in branches.Where(x => 
                        !string.IsNullOrEmpty(x.Image) && (StoryBookInstance == null || 
                        StoryBookInstance.ShowBranch(x))))
                {
                    branch.GameObj = new GameObject();
                    branch.GameObj.name = "Branch " + branch.NextPageId;
                    branch.GameObj.transform.SetParent(transform);
                }
            }
            _isUIDirty = true;

            if(Camera.allCamerasCount > 0)
                setupui();
        }
    }
}
