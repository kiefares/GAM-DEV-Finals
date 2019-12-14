using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Handles views in the game in stack order. Based from Dragon Cubes UI
/// By: NeilDG
/// </summary>
public class ViewHandler : MonoBehaviour {

	private static ViewHandler sharedInstance = null;
	public static ViewHandler Instance {
		get {
			return sharedInstance;
		}
	}

	[SerializeField] private GameObject uiRoot;
	[SerializeField] private string screenPrefabLocation = "UI/Screens";
	[SerializeField] private string firstScreen;
	
	[SerializeField] private List<View> activeViews = new List<View>();
	[SerializeField] private List<View> viewPool = new List<View>();	//views that need not be destroyed should be placed here

	private View rootView;
	private bool isUIActionsPermitted = true;

	public bool IsUIActionsPermitted
	{
		get
		{
			return isUIActionsPermitted;
		}
	}

	void Awake() {
		sharedInstance = this;
	}

	// Use this for initialization
	void Start () {
		this.Show(this.firstScreen);
	}

	void OnDestroy() {

	}
	
	// Update is called once per frame
	void Update () {
		if(this.isUIActionsPermitted && Input.GetKeyDown(KeyCode.Escape)) {
			this.OnBack ();
		}
	}
		

	public void RestrictUIActions() {
		this.isUIActionsPermitted = false;
	}

	public void AllowUIActions() {
		this.isUIActionsPermitted = true;
	}

	public Transform GetUIRoot() {
		return this.uiRoot.transform;
	}

	/// <summary>
	/// Show the specified screen by name.
	/// </summary>
	/// <param name="screenName">Screen name.</param>
	/// <param name="unique">If set to <c>true</c>, create a new instance/duplicate view.</param>
	public View Show(string screenName, bool unique = false) {

		if(screenName == "") {
			return null;
		}

		/*if(this.isUIActionsPermitted == false) {
			return;
		}*/

		View view;
		if(this.IsViewExisting(screenName) && unique == false) {
			view = this.FindActiveView(screenName);
			this.activeViews.Remove(view);
			this.activeViews.Add(view);
		}
		else if(this.IsViewInPool(screenName) && unique == false) {
			view = this.FindViewInPool(screenName);
			this.viewPool.Remove(view);
			this.activeViews.Add(view);
			
		}
		else {
			view = this.InitializeView(screenName);
			this.activeViews.Add(view);
		}

		view.Show();
		
		if(view.IsRootScreen()) {
			this.rootView = view;
		}

		this.RearrangeOverlay();

		if(view.IsRootScreen() == false) {
			
		}

		return view;
	}

	public void AddActiveView(View view) {
		this.activeViews.Add(view);
	}

	public void HideCurrentView() {
		if(this.isUIActionsPermitted == false) {
			return;
		}

		this.OnBack();
	}

	private void OnBack() {

		View activeView = this.GetActiveView();

		if(activeView == null || activeView.IsCancelable() == false) {
			return;
		}

		activeView.OnBackButtonPressed();
		if(activeView.IsRootScreen() == false) {
			activeView.Hide();
		}
	}

	public View GetActiveView() {
		if(this.activeViews.Count == 0) {
			return null;
		}
		else {
			return this.activeViews[this.activeViews.Count - 1];
		}
	}

	/// <summary>
	/// Returns the first instance of the active view found. Not recommended to use for multiple instances like popups.
	/// </summary>
	/// <returns>The active view.</returns>
	/// <param name="screenName">Screen name.</param>
	public View FindActiveView(string screenName) {
		foreach(View view in this.activeViews) {
			if(view.GetName() == screenName) {
				return view;
			}
		}
		
		return null;
	}


	/// <summary>
	/// Instantiates the view and then puts it temporarily in a view pool for faster retrieval. Use this for process-intensive
	/// views that consumes quite an amount of frame rate upon instantiating.
	/// 
	/// Automatically hides the view when called.
	/// </summary>
	/// <param name="screenName">Screen name.</param>
	public void PutToPool(string screenName) {
		if(this.IsViewExisting(screenName)){
			View view = this.FindActiveView(screenName);
			this.activeViews.Remove(view);
			
			if(!this.viewPool.Contains(view)) {
				view.DoNotDestroy();
				this.viewPool.Add(view);
			}
			
			view.SetVisibility(false);
			
		}
		else {
			View view= this.InitializeView(screenName);
			view.DoNotDestroy();
			this.viewPool.Add(view);

			view.SetVisibility(false);
		}
	}
	
	public void PutToPool(View view) {
		if(!this.viewPool.Contains(view)) {
			this.viewPool.Add(view);
		}
	}
	
	public bool IsViewInPool(string screenName) {
		for(int i = 0; i < this.viewPool.Count; i++) {
			View view = this.viewPool[i];
			
			if(view.GetName() == screenName) {
				return true;
			}
		}
		
		return false;
	}
	
	public View FindViewInPool(string screenName) {
		for(int i = 0; i < this.viewPool.Count; i++) {
			View view = this.viewPool[i];
			
			if(view.GetName() == screenName) {
				return view;
			}
		}
		
		return null;
	}

	public bool IsViewExisting(string screenName) {
		foreach(View view in this.activeViews) {
			if(view.GetName() == screenName) {
				return true;
			}
		}
		
		return false;
	}
	
	public bool IsViewActive(string screenName) {
		return (this.activeViews.Count != 0 && this.activeViews[this.activeViews.Count - 1].GetName() == screenName);
	}
	
	public void ClearPopupViews() {
		foreach(View view in this.activeViews) {
			if(!view.IsRootScreen()) {
				view.OnHideStarted();
				view.OnHideCompleted();
				GameObject.Destroy(view.gameObject);
			}
		}
		
		this.activeViews.Clear();
		this.activeViews.Add(this.rootView);
	}

	private View InitializeView(string screenName) 
	{
		View view;

		GameObject prefabObj = this.GetScreenPrefab(screenName);

		if(prefabObj == null)
		{
			Debug.LogWarning("Missing screen prefab : " + screenName);
			return null;
		}

		GameObject gObj = GameObject.Instantiate(prefabObj) as GameObject;
		gObj.transform.parent = uiRoot.transform;
		gObj.transform.localScale = Vector3.one;
		gObj.transform.localPosition = Vector3.zero;
		gObj.transform.localRotation = Quaternion.identity;
		gObj.name = screenName;
		gObj.SetActive(false);
		
		view = gObj.GetComponent<View>();
		
		return view;
	}

	private GameObject GetScreenPrefab(string screenName)
	{
		
		UnityEngine.Object obj = Resources.Load(screenPrefabLocation + "/" + screenName);
		if (obj == null) return null;
		
		GameObject gObj = obj as GameObject;
		return gObj;
	}

	/// <summary>
	/// Shows the view with null property. This removes the functionality of the view
	/// Only to be used for debugging.
	/// </summary>
	public void ShowViewWithNullProperty(string screenName) {
		Debug.LogWarning("Show " + screenName);

		if(this.isUIActionsPermitted == false) {
			return;
		}

		View view;
		if(this.IsViewExisting(screenName)) {
			view = this.FindActiveView(screenName);
			this.activeViews.Remove(view);
			this.activeViews.Add(view);
		}
		else {
			view = this.InitializeView(screenName);
			this.activeViews.Add(view);
		}
		
		this.ReplaceActiveViewWithNull();
		View activeView = this.GetActiveView();
		activeView.Show();
		
		if(view.IsRootScreen()) {
			this.rootView = view;
		}

		this.RearrangeOverlay();
	}
	
	private void ReplaceActiveViewWithNull() {
		GameObject viewObject = this.activeViews[this.activeViews.Count - 1].gameObject;
		NullScreen nullScreen = viewObject.AddComponent<NullScreen>();
		View originalView = this.activeViews[this.activeViews.Count - 1];
		nullScreen.CopyProperty(originalView);
		
		//GameObject.Destroy(viewObject.GetComponent<Animation>());
		GameObject.Destroy(this.activeViews[this.activeViews.Count - 1]);
		this.activeViews[this.activeViews.Count - 1] = nullScreen;
	}

	public void OnViewHidden(View view) {
		this.activeViews.Remove(view);
		
		if(view.ShouldBeDestroyed()) {
			//Debug.Log("DESTROYED " +view.GetName());
			GameObject.Destroy(view.gameObject);
			Resources.UnloadUnusedAssets();
			System.GC.Collect();
		}
		else {
			this.PutToPool(view);
			view.SetVisibility(false);
		}
		
		View newActiveView = this.GetActiveView();
		if(newActiveView != null) {
			newActiveView.SetVisibility(true);
			newActiveView.OnShowStarted();
			newActiveView.OnShowCompleted();
		}

		this.RearrangeOverlay();
		this.AllowUIActions();
		
	}

	private void RearrangeOverlay() {
		View activeView = this.GetActiveView();
		if(activeView == null || activeView.IsRootScreen()) {
			BlackOverlay.Hide();
			//EventBroadcaster.Instance.PostEvent(EventNames.ON_MAP_PAN_ENABLED);
		}
		else {
			BlackOverlay.Show(activeView.transform);
		}
	}
}
