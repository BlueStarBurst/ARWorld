//
//  ContentView.swift
//  sandbox
//
//  Created by David Peicho on 1/21/21.
//

import SwiftUI
import Firebase


struct ContentView: View {
    @Environment(\.scenePhase) private var phase
    @State var page = (Auth.auth().currentUser?.uid != nil) ? 1 : 0
    @State var isLoaded = true
    
    var body: some View {
        ZStack {
            if (page == 0) {
                AuthView(page: $page)
            } else if (page == 1) {
                UnityView(isLoaded: $isLoaded)
            }
        }.onAppear {
            setupColorScheme()
        }
    }
    
    private func signOut() {
        do {
            try Auth.auth().signOut()
        } catch let signOutError as NSError {
            print("Error signing out: %@", signOutError)
        }
    }
    
    private func setupColorScheme() {
        // We do this via the window so we can access UIKit components too.
        let window = UIApplication.shared.windows.first
        window?.overrideUserInterfaceStyle = .dark
        window?.tintColor = UIColor(Color.pink)
    }}
