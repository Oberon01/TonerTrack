import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterModule, RouterOutlet],
  template: `
    <div class="flex h-screen bg-gray-50">

      <!-- Sidebar -->
      <aside class="w-56 bg-gray-900 flex flex-col flex-shrink-0">

        <!-- Logo -->
        <div class="px-4 py-5 border-b border-gray-700">
          <div class="flex items-center gap-2">
            <span class="text-2xl">🖨</span>
            <div>
              <h1 class="text-white font-bold text-base leading-tight">TonerTrack</h1>
              <p class="text-gray-400 text-xs">Printer Monitoring</p>
            </div>
          </div>
        </div>

        <!-- Nav links -->
        <nav class="flex-1 px-3 py-4 space-y-1">
          <a
            routerLink="/dashboard"
            routerLinkActive="bg-gray-700 text-white"
            [routerLinkActiveOptions]="{ exact: false }"
            class="flex items-center gap-3 px-3 py-2 rounded-lg text-gray-300
                   hover:bg-gray-700 hover:text-white transition-colors text-sm">
            <svg class="w-4 h-4 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M4 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2V6z
                   M14 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V6z
                   M4 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2v-2z
                   M14 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z" />
            </svg>
            Dashboard
          </a>

          <a
            routerLink="/printers"
            routerLinkActive="bg-gray-700 text-white"
            [routerLinkActiveOptions]="{ exact: false }"
            class="flex items-center gap-3 px-3 py-2 rounded-lg text-gray-300
                   hover:bg-gray-700 hover:text-white transition-colors text-sm">
            <svg class="w-4 h-4 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M17 17H17.01M17 3H5a2 2 0 00-2 2v11a2 2 0 002 2h14a2 2 0
                   002-2V5a2 2 0 00-2-2zM11 7l2 2-2 2" />
            </svg>
            Printers
          </a>

          <a
            routerLink="/discovery"
            routerLinkActive="bg-gray-700 text-white"
            [routerLinkActiveOptions]="{ exact: false }"
            class="flex items-center gap-3 px-3 py-2 rounded-lg text-gray-300
                   hover:bg-gray-700 hover:text-white transition-colors text-sm">
            <svg class="w-4 h-4 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
            </svg>
            Discovery
          </a>
        </nav>

        <!-- Footer -->
        <div class="px-4 py-3 border-t border-gray-700">
          <p class="text-gray-500 text-xs">TonerTrack</p>
        </div>

      </aside>

      <!-- Main content area -->
      <main class="flex-1 overflow-auto min-w-0">
        <router-outlet />
      </main>

    </div>
  `,
})
export class AppComponent {}
